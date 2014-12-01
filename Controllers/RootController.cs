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
    public class RootController : BaseController {

        //[OutputCache(Duration = 0)]
        //public ActionResult Login() {
        //    return View();
        //}

        public ActionResult Logout() {
            FormsAuthentication.SignOut();
            TempData["flash"] = "You've been logged out";
            return RedirectToAction("login");
        }

        public ActionResult Login(string username, string password, string returnUrl) {
            var passhash = ConfigurationManager.AppSettings["password"];

            if(username == "admin" && BCryptHelper.CheckPassword(password, passhash)) {
                int oneDay = 24 * 60;
                var ticket = new FormsAuthenticationTicket(username, false, oneDay);

                HttpCookie cookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket));
                Response.Cookies.Add(cookie);

                LogService.Info("successful login for " + username);
                if(!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);
                return RedirectToAction("index");
            }

            LogService.Warn("failed login attempt for " + username);
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