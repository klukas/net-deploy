using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Configuration;
using System.Text.RegularExpressions;

namespace deploy.Models {
	public class Builder {


		string _id;
		string _appdir;
		Dictionary<string, string> _config;
		string _logfile;
		string _sourcedir;

		public Builder(string id) {
			_id = id;
		}


		public void Build() {
			lock(AppLock.Get(_id)) {
				Init();

				FileDB.AppState(_id, "building");
				try {					
					GitUpdate();
					NugetRefresh();
					Msbuild();
					Deploy();

					Log("-> build completed");
					FileDB.AppState(_id, "idle");
				} catch(Exception e) {
					Log("ERROR: " + e.ToString());
					Log("-> build failed!");
					FileDB.AppState(_id, "failed");
					throw;
				}

			}
		}

		private void Init() {
			var state = FileDB.AppState(_id);
			if(state.Item2 != "idle" && state.Item2 != "failed") throw new Exception("Can't build: current state is " + state.Item2);

			_appdir = FileDB.AppDir(_id);
			_config = FileDB.AppConfig(_id);
			_logfile = Path.Combine(_appdir, "log.txt");
			_sourcedir = Path.Combine(_appdir, "source");

			if(File.Exists(_logfile)) File.Delete(_logfile); // clear log
		}

		private void GitUpdate() {
			var giturl = _config["git"];
			if(string.IsNullOrEmpty(giturl)) throw new Exception("git missing from config");

			if(!Directory.Exists(_sourcedir)) {
				Directory.CreateDirectory(_sourcedir);
				Log("-> doing git clone");
				Cmd.Run("git clone " + giturl + " source", runFrom: _appdir, logPath: _logfile).EnsureCode(0);
			} else {
				Log("-> doing git pull");
				Cmd.Run("git pull " + giturl, runFrom: _sourcedir, logPath: _logfile).EnsureCode(0);
			}
		}

		private void NugetRefresh() {
			Log("-> doing nuget refresh");
			Cmd.Run("echo off && for /r . %f in (packages.config) do if exist %f echo found %f && nuget i \"%f\" -o packages", runFrom: _sourcedir, logPath: _logfile)
				.EnsureCode(0);
		}

		private void Msbuild() {
			var msbuild = ConfigurationManager.AppSettings["msbuild"];
			string build_config = null;
			_config.TryGetValue("build_config", out build_config);

			Log("-> building with " + msbuild);

			string parameters = "";
			if(build_config != null) {
				parameters += " /p:Configuration=" + build_config;
			}

			Cmd.Run("\"" + msbuild + "\"" + parameters, runFrom: _sourcedir, logPath: _logfile)
				.EnsureCode(0);
		}

		private void Deploy() {
			string deploy_base, deploy_to, deploy_ignore = null;

			_config.TryGetValue("deploy_base", out deploy_base);
			_config.TryGetValue("deploy_to", out deploy_to);
			_config.TryGetValue("deploy_ignore", out deploy_ignore);

			if(string.IsNullOrWhiteSpace(deploy_to)) throw new Exception("deploy_to not specified in config");

			Log(" -> deploying to " + deploy_to);

			var source = string.IsNullOrEmpty(deploy_base) ? _sourcedir : Path.Combine(_sourcedir, deploy_base);

			if(!Directory.Exists(deploy_to)) Directory.CreateDirectory(deploy_to);

			List<string> simple;
			List<string> paths;
			GetIgnore(source, deploy_to, deploy_ignore, out simple, out paths);
			Log("got ignore paths");

			var xf = new List<string>(simple);
			var xd = new List<string>(simple);
			xd.AddRange(paths);

			var xf_arg = xf.Count > 0 ? " /xf " + string.Join(" ", xf) : null;
			var xd_arg = xd.Count > 0 ? " /xd " + string.Join(" ", xd) : null;

			Cmd.Run("\"robocopy . \"" + deploy_to + "\" /s /purge /nfl /ndl " + xf_arg + xd_arg + "\"", runFrom: source, logPath: _logfile);
		}

		private void GetIgnore(string source, string dest, string ignore_str, out List<string> simple, out List<string> paths) {
			simple = new List<string>();
			paths = new List<string>();

			if(string.IsNullOrWhiteSpace(ignore_str)) return; // nothing to ignore

			var ignore = Regex.Split(ignore_str, @"(?<!\\)\s+"); // split on spaces, unless they're escaped with \

			var path_segments = new List<string>();
			foreach(var i in ignore) {
				var part = i.Replace(@"\ ", " ");
				if(part.Contains("\\")) path_segments.Add(part);
				else simple.Add(QuoteSpacesInPath(part));
			}

			if(path_segments.Count > 0) {
				// have to manually look for directories that match the path segment
				foreach(var seg in path_segments) {
					var sourcepath = Path.Combine(source, seg);
					if(File.Exists(sourcepath) || Directory.Exists(sourcepath)) paths.Add(QuoteSpacesInPath(sourcepath.TrimEnd('\\')));
					else {
						var destpath = Path.Combine(dest, seg);
						if(File.Exists(destpath) || Directory.Exists(destpath)) paths.Add(QuoteSpacesInPath(destpath.TrimEnd('\\')));
					}
				}
			}
		}

		private static string QuoteSpacesInPath(string path) {
			return Regex.Replace(path, @"^(\S*\s+)(\S*\s*)*", "\"$&\"");
		}

		private void Log(string message) {
			File.AppendAllText(_logfile, message + "\r\n");
		}
	}
}