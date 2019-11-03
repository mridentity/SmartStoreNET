using System.Web.Mvc;
using System.Web.Routing;
using ReadySignOn.ReadyPay;
using ReadySignOn.ReadyPay.Services;
using SmartStore.Web.Framework.Routing;

namespace ReadySignOn.ReadyPay
{
	public class RouteProvider : IRouteProvider
	{
		public void RegisterRoutes(RouteCollection routes)
		{
			routes.MapRoute("ReadySignOn.ReadyPay",
                    "Plugins/ReadySignOn.ReadyPay/{controller}/{action}",
					new { controller = "ReadyPay" },
					new[] { "ReadySignOn.ReadyPay.Controllers" }
			)
			.DataTokens["area"] = Plugin.SystemName;

			// for backward compatibility (IPN!)
			routes.MapRoute("ReadySignOn.ReadyPay.Legacy",
					"Plugins/PaymentsReadyPay/{action}",
					new { controller = "ReadyPay" },
					new[] { "ReadySignOn.ReadyPay.Controllers" }
			)
			.DataTokens["area"] = Plugin.SystemName;
		}

		public int Priority { get { return 0; } }
	}
}