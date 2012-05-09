using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Configuration;

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

					Log("-> build completed");
					FileDB.AppState(_id, "idle");
				} catch {
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
				new Cmd("git clone " + giturl + " source", runFrom: _appdir, logPath: _logfile).Run().EnsureCode(0);
			} else {
				Log("-> doing git pull");
				new Cmd("git pull " + giturl, runFrom: _sourcedir, logPath: _logfile).Run().EnsureCode(0);
			}
		}

		private void NugetRefresh() {
			Log("-> doing nuget refresh");
			new Cmd("echo off && for /r . %f in (packages.config) do if exist %f echo found %f && nuget i \"%f\" -o packages", runFrom: _sourcedir, logPath: _logfile)
				.Run().EnsureCode(0);
		}

		private void Msbuild() {
			var msbuild = ConfigurationManager.AppSettings["msbuild"];

			Log("-> building with " + msbuild);
			new Cmd("\"" + msbuild + "\"", runFrom: _sourcedir, logPath: _logfile)
				.Run().EnsureCode(0);
		}

		private void Log(string message) {
			File.AppendAllText(_logfile, message + "\r\n");
		}
	}
}