using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;

namespace deploy {
	public class Temp {
		public static void Test() {
			var url = "https://lukesampson:s3cr3t@github.com/repo";
			Console.WriteLine(Regex.Replace(url, @"(git|ssh|https|http|ftps|ftp)(://[^:]+:)([^@]+)(@)", "$1$2*****$4"));
		}

		public static void Test2() {
			Console.WriteLine(Regex.Replace(@"C:\Users\Luke Sampson\Projects\deploy\App_Data\studystays\source\Web\Uploads\Sites", @"^(\S*\s+)(\S*\s*)*", "\"$&\""));
		}
	}
}