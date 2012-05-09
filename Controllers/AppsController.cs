using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Dynamic;
using deploy.Models;
using System.Threading.Tasks;

namespace deploy.Controllers {
	public class AppsController : Controller {

		public ActionResult Detail(string id) {
			ViewBag.id = id;
			ViewBag.state = FileDB.AppState(id);
			ViewBag.config = FileDB.AppConfig(id);

			return View();
		}

		[HttpPost]
		public ActionResult Build(string id) {
			var context = System.Web.HttpContext.Current;
			var builder = new Builder(id);
			new Task(() => {
				try {
					builder.Build();
				} catch(Exception e) {
					Elmah.ErrorLog.GetDefault(context).Log(new Elmah.Error(e));
				}
			}).Start();;

			return RedirectToAction("detail", new { id = id });
		}

	}
}
