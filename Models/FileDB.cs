using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Configuration;

namespace deploy.Models {
	public static class FileDB {
		static object sync = new object();
		static string datadir = Path.Combine(HostingEnvironment.ApplicationPhysicalPath, "App_Data");

		public static List<string> Apps() {
			lock(sync) {
				return new DirectoryInfo(datadir).GetDirectories().Select(di => di.Name).ToList();
			}
		}

		public static string AppDir(string id) {
			return Path.Combine(datadir, id);
		}

		public static Tuple<DateTime?, string> AppState(string id) {
			lock(sync) {
				var statefile = Path.Combine(AppDir(id), "state.txt");
				if(!File.Exists(statefile)) return Tuple.Create(null as DateTime?, "idle");

				var state = File.ReadAllText(statefile).Split(' ');
				if(state.Length != 2) return Tuple.Create(null as DateTime?, "corrupt");

				DateTime when;
				if(!DateTime.TryParseExact(state[0], "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out when))
					return Tuple.Create(null as DateTime?, "corrupt");

				return Tuple.Create(when as DateTime?, state[1]);
			}
		}

		public static void AppState(string id, string state) {
			lock(sync) {
				var statefile = Path.Combine(AppDir(id), "state.txt");
				File.WriteAllText(statefile, DateTime.Now.ToString("yyyyMMddHHmmss") + " " + state);
			}
		}

		public static Dictionary<string, string> AppConfig(string id) {
			lock(sync) {
				var config = new Dictionary<string, string>();
				var configfile = Path.Combine(AppDir(id), "config.txt");
				var lineno = 1;
				var source = File.ReadAllText(configfile);

                // defaults
                config["msbuild"] = ConfigurationManager.AppSettings["msbuild"];

				foreach(var line in Regex.Split(source, "\n")) {
					var pair = line.Split('=');
					if(pair.Length != 2)
						throw new HttpParseException("Error parsing config.txt for " + id + ": expected name=val", null, configfile, source, lineno);

					config[pair[0].Trim()] = pair[1].Trim();
					lineno++;
				}
				return config;
			}
		}

		public static string LogPath(string id) {
			return Path.Combine(AppDir(id), "log.txt");
		}

		public static DateTime? LogCreated(string id) {
			lock(sync) {
				var path = LogPath(id);
				if(!File.Exists(path)) return null;

				return new FileInfo(path).CreationTime;
			}
		}

		/// <summary>
		/// Makes sure passwords are escaped before being displayed
		/// </summary>
		public static Dictionary<string, string> SanitizeForDisplay(this Dictionary<string, string> config) {
			var sane = new Dictionary<string, string>(config);

			var password = new Regex(@"(git|ssh|https|http|ftps|ftp)(://[^:]+:)([^@]+)(@)", RegexOptions.IgnoreCase);

			foreach(var key in sane.Keys.ToArray()) {
				sane[key] = password.Replace(sane[key], "$1$2*****$4");
			}

			return sane;
			
		}
	}
}