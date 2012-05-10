using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace deploy.Controllers {

	[UseRequireHttpsFromAppSettings]
	public abstract class BaseController : Controller {
	}
}
