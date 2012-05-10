using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using DevOne.Security.Cryptography.BCrypt;
using System.Text;

namespace deploy {
	public class Util {

		public static string BcryptPassword(string password) {
			var salt = BCryptHelper.GenerateSalt(10);
			var hash = BCryptHelper.HashPassword(password, salt);
			return hash;
		}

		// you can run this from Visual Studio if you have the TestDriven.NET plugin
		public static void BcryptPassword() {
			// put your password here
			var password = "password";
			Console.WriteLine(BcryptPassword(password));
		}
	}
}