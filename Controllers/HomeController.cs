using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using deploy.Models;
using System.Web.Security;

namespace deploy.Controllers {
	public class HomeController : Controller {

		public ActionResult Login() {
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Login(string username, string password) {
			if(username != "admin" || password != "br3akAway") {
				TempData["flash"] = "invalid login";
				return View();
			}

			Response.Cookies.Add(FormsAuthentication.GetAuthCookie(username, true));

			return RedirectToAction("index");
		}

		[Authorize]
		public ActionResult Index() {
			var apps = FileDB.Apps();

			return View(apps);
		}

	}
}
