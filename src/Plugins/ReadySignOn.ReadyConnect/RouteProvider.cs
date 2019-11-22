using SmartStore.Web.Framework.Routing;
using System.Web.Mvc;
using System.Web.Routing;

namespace ReadySignOn.ReadyConnect
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            routes.MapRoute("ReadySignOn.ReadyConnect",
                 "Plugins/ReadySignOn.ReadyConnect/{action}",
				 new { controller = "ExternalAuthReadyConnect" },
				 new[] { "ReadySignOn.ReadyConnect.Controllers" }
			)
			.DataTokens["area"] = ReadyConnectExternalAuthMethod.SystemName;
        }

        public int Priority => 0;
    }
}