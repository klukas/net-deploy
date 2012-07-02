using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using deploy.Models;
using System.Web.Security;
using DevOne.Security.Cryptography.BCrypt;
using System.Configuration;
using System.Web.UI;

namespace deploy.Controllers {
	public class HomeController : BaseController {

        [OutputCache(Duration=0)]
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
				int oneDay = 24 * 60;
				var ticket = new FormsAuthenticationTicket(username, false, oneDay);

				HttpCookie cookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket));
				Response.Cookies.Add(cookie);

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
