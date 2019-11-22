using ReadySignOn.ReadyConnect.Core;
using SmartStore.Core.Domain.Customers;
using SmartStore.Services.Authentication.External;
using SmartStore.Web.Framework.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ReadySignOn.ReadyConnect.Controllers
{
    public class ExternalAuthReadyConnectController : PluginControllerBase
    {
        private readonly IOAuthProviderReadyConnectAuthorizer _oAuthProviderReadyConnectAuthorizer;
        private readonly IOpenAuthenticationService _openAuthenticationService;
        private readonly ExternalAuthenticationSettings _externalAuthenticationSettings;

        public ExternalAuthReadyConnectController(
            IOAuthProviderReadyConnectAuthorizer oAuthProviderReadyConnectAuthorizer,
            IOpenAuthenticationService openAuthenticationService,
            ExternalAuthenticationSettings externalAuthenticationSettings)
        {
            _oAuthProviderReadyConnectAuthorizer = oAuthProviderReadyConnectAuthorizer;
            _openAuthenticationService = openAuthenticationService;
            _externalAuthenticationSettings = externalAuthenticationSettings;
        }
    }
}