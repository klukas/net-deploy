using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Web.Hosting;

namespace deploy.Models {
	public class Builder {
		string _id;
		string _appdir;
		Dictionary<string, string> _config;
		string _logfile;
		string _sourcedir;
        string _workingdir;
        string _buildconfig;

		public Builder(string id) {
			_id = id;
		}

		public void Build() {
			lock(AppLock.Get(_id)) 
            {
				Init();

                try 
                {
                    FileDB.AppState(_id, "building");

					GitUpdate();
					//NugetRefresh();
                    CopyWorking();
                    //Transform();
					Msbuild();
					Deploy();
                    Deploy_Referenzes();
                    Deploy_Test();
                    Deploy_Production();
                    Delete_Folder();

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
            _workingdir = Path.Combine(_appdir, "working");

            if(!_config.TryGetValue("build_config", out _buildconfig)) _buildconfig = "Release";

			if(File.Exists(_logfile)) File.Delete(_logfile); // clear log

            if (Directory.Exists(_sourcedir))
                Cmd.Run("\"rmdir " + _sourcedir + " /s /q \"", runFrom: _appdir);
		}

		private void GitUpdate() {
			var giturl = _config["git"];
            var gitPullUrl = _config["pull_git"];

            // optional GitHub command
            if (!string.IsNullOrEmpty(giturl) && !string.IsNullOrEmpty(gitPullUrl))
            {
                if (!Directory.Exists(_sourcedir))
                {
                    Directory.CreateDirectory(_sourcedir);
                    Log("-> doing git clone");
                    Cmd.Run("git clone " + giturl + " source", runFrom: _appdir, logPath: _logfile).EnsureCode(0);
                }
                else
                {
                    Log("-> doing git pull");
                    Cmd.Run("git pull " + gitPullUrl + " source", runFrom: _sourcedir, logPath: _logfile).EnsureCode(0);
                }
            }
            else
            {
                Log("-> skip GitHub - get or update!");
            }
		}

		private void NugetRefresh() {
			Log("-> doing nuget refresh");
			Cmd.Run("echo off && for /r . %f in (packages.config) do if exist %f echo found %f && nuget i \"%f\" -o packages", runFrom: _sourcedir, logPath: _logfile)
				.EnsureCode(0);
		}

        private void CopyWorking() 
        {
            string deploy_from = null;

            _config.TryGetValue("deploy_from", out deploy_from);

            if(!Directory.Exists(_workingdir)) Directory.CreateDirectory(_workingdir);

            if (!string.IsNullOrEmpty(deploy_from))
                Cmd.Run("\"robocopy . \"" + _workingdir + "\" /s /purge /nfl /ndl \"", runFrom: deploy_from, logPath: _logfile);
            else
                Cmd.Run("\"robocopy . \"" + _workingdir + "\" /s /purge /nfl /ndl /xd bin obj .git\"", runFrom: _sourcedir, logPath: _logfile);

            Log("-> copying to working dir finish.");
        }

        private void Delete_Folder()
        {
            if (Directory.Exists(_sourcedir))
                Cmd.Run("\"rmdir " + _sourcedir + " /s /q \"", runFrom: _appdir);
        }

        private void Transform() {
            Log("-> running web.config transforms");

            var msbuild = ConfigurationManager.AppSettings["msbuild"];
            var scriptpath = ConfigurationManager.AppSettings["buildscripts"];

            var candidates = Directory.GetFiles(_workingdir, "web.config", SearchOption.AllDirectories);
            Log("found " + candidates.Length + " web.config files");
            foreach(var webConfig in candidates) {

                var dir = Path.GetDirectoryName(webConfig);
                var transform = Path.Combine(dir, "web." + _buildconfig + ".config");
                if(File.Exists(transform)) 
                {
                    string cmd_string = String.Format("\"{0}\" /p:Dir=\"{1}\" /p:Source={2} /p:Transform={3} {4}",
                        msbuild, dir, Path.GetFileName(webConfig), Path.GetFileName(transform), Path.Combine(scriptpath, "transform.msbuild"));

                    Cmd.Run(cmd_string, _logfile).EnsureCode(0);

                    // delete transforms
                    foreach(var trsfm in Directory.GetFiles(dir, "web.*.config", SearchOption.TopDirectoryOnly)) {
                        File.Delete(trsfm);
                    }
                    File.Delete(webConfig);
                    File.Move(webConfig + ".transformed", webConfig);
                }
            }
        }

		private void Msbuild() 
        {
            // optional building
            if (!string.IsNullOrEmpty(_config["ms_solution"]))
            {
                var msbuild = _config["msbuild"];
                var msSolution = _config["ms_solution"];

                Log("-> building with " + msbuild + " (" + _buildconfig + " config)");

                string parameters = "";
                if (_buildconfig != null)
                {
                    parameters += " /p:Configuration=" + _buildconfig;
                }

                string cmd_string = String.Format("\"{0}\" " + msSolution + " /t:Rebuild /verbosity:minimal ", msbuild);

                Cmd.Run(cmd_string, runFrom: _workingdir, logPath: _logfile).EnsureCode(0);
            }
            else
            {
                Log("-> skip ms - build!");
            }
		}

		private void Deploy() 
        {
            string deploy_base, deploy_to, deploy_ignore = null;

			_config.TryGetValue("deploy_base", out deploy_base);
			_config.TryGetValue("deploy_to", out deploy_to);
			_config.TryGetValue("deploy_ignore", out deploy_ignore);

			if(string.IsNullOrWhiteSpace(deploy_to)) throw new Exception("deploy_to not specified in config");

            var source = string.IsNullOrEmpty(deploy_base) ? _workingdir : Path.Combine(_workingdir, deploy_base);
            
            Log(string.Format(" -> deploy from {0} and deploying to {1} ", source, deploy_to));

			//if(!Directory.Exists(deploy_to)) Directory.CreateDirectory(deploy_to);

			List<string> simple;
			List<string> paths;
			GetIgnore(source, deploy_to, deploy_ignore, out simple, out paths);

			var xf = new List<string>(simple);
			var xd = new List<string>(simple);
			xd.AddRange(paths);

			var xf_arg = xf.Count > 0 ? " /xf " + string.Join(" ", xf) : null;
			var xd_arg = xd.Count > 0 ? " /xd " + string.Join(" ", xd) : null;

            foreach (string path in deploy_to.Split('|'))
            {
                Cmd.Run("\"robocopy . " + path.Trim() + " /s /purge /nfl /ndl " + xf_arg + xd_arg + "\"", runFrom: source, logPath: _logfile);
            }

            Log("-> deploy complete!");
		}

        private void Deploy_Referenzes()
        {
            string url_referenz, url_backend, url_backend_service = string.Empty;

            _config.TryGetValue("deploy_referenz", out url_referenz);
            _config.TryGetValue("deploy_backend", out url_backend);
            _config.TryGetValue("deploy_backend_services", out url_backend_service);

            if (!string.IsNullOrEmpty(url_referenz) && !string.IsNullOrEmpty(url_backend) && !string.IsNullOrEmpty(url_backend_service))
            {
                Cmd.Run("\"robocopy . \"" + url_backend + "\" /s /purge /nfl /ndl /xf *.log \"", runFrom: url_referenz, logPath: _logfile);
                Cmd.Run("\"robocopy . \"" + url_backend_service + "\" /s /purge /nfl /ndl /xf *.log \"", runFrom: url_referenz, logPath: _logfile);
            }
            else
            {
                Log("-> skip deploy references!");
            }
        }

        private void Deploy_Test()
        {
            string url_referenz, url_test = string.Empty;
            string deploy_ignore = null;

            _config.TryGetValue("deploy_referenz", out url_referenz);
            _config.TryGetValue("deploy_test", out url_test);
            _config.TryGetValue("deploy_test_ignore", out deploy_ignore);

            if (!string.IsNullOrEmpty(url_referenz) && !string.IsNullOrEmpty(url_test) && !string.IsNullOrEmpty(deploy_ignore))
            {
                Cmd.Run("\"robocopy . \"" + url_test + "\" /s /purge /nfl /ndl /xf " + deploy_ignore + " \"", runFrom: url_referenz, logPath: _logfile);
            }
            else
            {
                Log("-> skip deploy test!");
            }
        }

        private void Deploy_Production()
        {
            string deploy_production, deploy_from = null;
            string deploy_ignore = null;

            _config.TryGetValue("deploy_production", out deploy_production);
            _config.TryGetValue("deploy_from", out deploy_from);
            _config.TryGetValue("deploy_ignore", out deploy_ignore);

            if (!string.IsNullOrEmpty(deploy_production) && !string.IsNullOrEmpty(deploy_from) && !string.IsNullOrEmpty(deploy_ignore))
            {
                foreach (string path in deploy_production.Split('|'))
                {
                    Cmd.Run("\"robocopy . \"" + path.Trim() + "\" /s /purge /nfl /ndl /xf " + deploy_ignore + " \"", runFrom: deploy_from, logPath: _logfile);
                }
            }
            else
            {
                Log("-> skip deploy production!");
            }
        }

        // TODO wth das geht nicht mehr
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