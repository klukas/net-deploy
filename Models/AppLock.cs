using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace deploy.Models {
	public static class AppLock {
		static Dictionary<string, object> locks = new Dictionary<string, object>();
		static object sync = new object();

		public static object Get(string id) {
			object l;
			lock(sync) {
				if(!locks.TryGetValue(id, out l)) {
					l = new object();
					locks.Add(id, l);
				}
				return l;
			}
		}

	}
}