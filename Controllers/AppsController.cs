using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Dynamic;
using deploy.Models;
using System.Threading.Tasks;

namespace deploy.Controllers {
	
	public class AppsController : BaseController {

		[Authorize]
		public ActionResult Detail(string id) {
			ViewBag.id = id;
			ViewBag.state = FileDB.AppState(id);
			ViewBag.config = FileDB.AppConfig(id).SanitizeForDisplay();
			ViewBag.logcreated = FileDB.LogCreated(id);

			return View();
		}

		[Authorize]
		[HttpPost]
		public ActionResult Build(string id) {
			var context = System.Web.HttpContext.Current;
			var builder = new Builder(id);
			new Task(() => {
				try {
					builder.Build();
				} catch(Exception e) {
                    LogService.Fatal(e);
				}
			}).Start();;

			return RedirectToAction("detail", new { id = id });
		}

		[Authorize]
		public ActionResult Log(string id) {
			var path = FileDB.LogPath(id);
			if(!System.IO.File.Exists(path)) return HttpNotFound("No log file found");

			return File(path, "text/plain");
		}

	}
}
