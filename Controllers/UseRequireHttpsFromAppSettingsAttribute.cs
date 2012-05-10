using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Configuration;

namespace deploy.Controllers {
	public class UseRequireHttpsFromAppSettingsAttribute : RequireHttpsAttribute {
		bool? _required;
		bool Required {
			get {
				if(!_required.HasValue) {
					_required = bool.Parse(ConfigurationManager.AppSettings["require_https"]);
				}
				return _required.Value;
			}
		}
		public override void OnAuthorization(AuthorizationContext filterContext) {
			if(Required) base.OnAuthorization(filterContext);
		}
	}
}