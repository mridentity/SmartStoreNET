using SmartStore.Core.Plugins;
using SmartStore.Services.Authentication.External;
using SmartStore.Services.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Routing;

namespace ReadySignOn.ReadyConnect
{
    public class ReadyConnectExternalAuthMethod : BasePlugin, IExternalAuthenticationMethod, IConfigurable
    {
        private readonly ILocalizationService _localizationService;

        public ReadyConnectExternalAuthMethod(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
        }

        public static string SystemName => "ReadySignOn.ReadyConnect";

        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "ExternalAuthReadyConnect";
            routeValues = new RouteValueDictionary(new { Namespaces = "ReadySignOn.ReadyConnect.Controllers", area = SystemName });
        }

        public void GetPublicInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "PublicInfo";
            controllerName = "ExternalAuthReadyConnect";
            routeValues = new RouteValueDictionary(new { Namespaces = "ReadySignOn.ReadyConnect.Controllers", area = SystemName });
        }

        public override void Install()
        {
            _localizationService.ImportPluginResourcesFromXml(PluginDescriptor);

            base.Install();
        }

        public override void Uninstall()
        {
            _localizationService.DeleteLocaleStringResources(PluginDescriptor.ResourceRootKey);

            base.Uninstall();
        }
    }
}