using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using deploy.Models;
using System.Web.Security;
using DevOne.Security.Cryptography.BCrypt;
using System.Configuration;

namespace deploy.Controllers {
	public class HomeController : BaseController {

		public ActionResult Login() {
			return View();
		}

		public ActionResult Logout() {
			FormsAuthentication.SignOut();
			TempData["flash"] = "You've been logged out";
			return RedirectToAction("login");
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Login(string username, string password, string returnUrl) {
			var passhash = ConfigurationManager.AppSettings["password"];

			if(username == "admin" && BCryptHelper.CheckPassword(password, passhash)) {
				var cookie = FormsAuthentication.GetAuthCookie(username, true);
				cookie.Expires = DateTime.MinValue; // makes it a session cookie
				Response.Cookies.Add(cookie);
				if(!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);
				return RedirectToAction("index");
			}

			TempData["flash"] = "invalid login";
			return View();
		}

		[Authorize]
		public ActionResult Index() {
			var apps = FileDB.Apps();

			return View(apps);
		}

	}
}
