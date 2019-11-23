using ReadySignOn.ReadyConnect.Core;
using ReadySignOn.ReadyConnect.Models;
using SmartStore;
using SmartStore.ComponentModel;
using SmartStore.Core.Domain.Customers;
using SmartStore.Core.Security;
using SmartStore.Services.Authentication.External;
using SmartStore.Web.Framework;
using SmartStore.Web.Framework.Controllers;
using SmartStore.Web.Framework.Security;
using SmartStore.Web.Framework.Settings;
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
        [LoadSetting, AdminAuthorize, ChildActionOnly]
        [Permission(Permissions.Configuration.Authentication.Read)]
        public ActionResult Configure(ReadyConnectExternalAuthSettings settings)
        {
            var model = new ConfigurationModel();
            MiniMapper.Map(settings, model);

            var host = Services.StoreContext.CurrentStore.GetHost(true);
            model.RedirectUrl = $"{host}Plugins/ReadySignOn.ReadyConnect/logincallback/";

            return View(model);
        }

        [SaveSetting, HttpPost, AdminAuthorize, ChildActionOnly]
        [Permission(Permissions.Configuration.Authentication.Update)]
        public ActionResult Configure(ReadyConnectExternalAuthSettings settings, ConfigurationModel model)
        {
            if (!ModelState.IsValid)
            {
                return Configure(settings);
            }

            MiniMapper.Map(model, settings);
            settings.ClientKeyIdentifier = model.ClientKeyIdentifier.TrimSafe();
            settings.ClientSecret = model.ClientSecret.TrimSafe();
            settings.UseSandbox = model.UseSandbox;

            NotifySuccess(T("Admin.Common.DataSuccessfullySaved"));

            return RedirectToConfiguration(ReadyConnectExternalAuthMethod.SystemName, true);
        }

        [ChildActionOnly]
        public ActionResult PublicInfo()
        {
            var settings = Services.Settings.LoadSetting<ReadyConnectExternalAuthSettings>(Services.StoreContext.CurrentStore.Id);

            if (settings.ClientKeyIdentifier.HasValue() && settings.ClientSecret.HasValue())
            {
                return View();
            }

            return new EmptyResult();
        }

        public ActionResult Login(string returnUrl)
        {
            return LoginInternal(returnUrl, false);
        }

        public ActionResult LoginCallback(string returnUrl)
        {
            return LoginInternal(returnUrl, true);
        }

        [NonAction]
        private ActionResult LoginInternal(string returnUrl, bool verifyResponse)
        {
            var processor = _openAuthenticationService.LoadExternalAuthenticationMethodBySystemName(ReadyConnectExternalAuthMethod.SystemName, Services.StoreContext.CurrentStore.Id);
            if (processor == null || !processor.IsMethodActive(_externalAuthenticationSettings))
            {
                NotifyError(T("Plugins.CannotLoadModule", T("Plugins.FriendlyName.ReadySignOn.ReadyConnect")));
                return new RedirectResult(Url.LogOn(returnUrl));
            }

            var viewModel = new LoginModel();
            TryUpdateModel(viewModel);

            var result = _oAuthProviderReadyConnectAuthorizer.Authorize(returnUrl, verifyResponse);
            switch (result.AuthenticationStatus)
            {
                case OpenAuthenticationStatus.Error:
                    {
                        if (!result.Success)
                        {
                            result.Errors.Each(x => NotifyError(x));
                        }

                        return new RedirectResult(Url.LogOn(returnUrl));
                    }
                case OpenAuthenticationStatus.AssociateOnLogon:
                    {
                        return new RedirectResult(Url.LogOn(returnUrl));
                    }
                case OpenAuthenticationStatus.AutoRegisteredEmailValidation:
                    {
                        return RedirectToRoute("RegisterResult", new { resultId = (int)UserRegistrationType.EmailValidation, returnUrl });
                    }
                case OpenAuthenticationStatus.AutoRegisteredAdminApproval:
                    {
                        return RedirectToRoute("RegisterResult", new { resultId = (int)UserRegistrationType.AdminApproval, returnUrl });
                    }
                case OpenAuthenticationStatus.AutoRegisteredStandard:
                    {
                        return RedirectToRoute("RegisterResult", new { resultId = (int)UserRegistrationType.Standard, returnUrl });
                    }
                default:
                    break;
            }

            if (result.Result != null)
            {
                return result.Result;
            }

            return HttpContext.Request.IsAuthenticated ?
                RedirectToReferrer(returnUrl, "~/") :
                new RedirectResult(Url.LogOn(returnUrl));
        }
    }
}