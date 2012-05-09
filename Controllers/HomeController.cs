using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using deploy.Models;
using System.Web.Security;

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
			if(username != "admin" || password != "br3akAway") {
				TempData["flash"] = "invalid login";
				return View();
			}

			Response.Cookies.Add(FormsAuthentication.GetAuthCookie(username, true));

			if(!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);

			return RedirectToAction("index");
		}

		[Authorize]
		public ActionResult Index() {
			var apps = FileDB.Apps();

			return View(apps);
		}

	}
}
