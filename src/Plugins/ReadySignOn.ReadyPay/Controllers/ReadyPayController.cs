using ReadySignOn.ReadyPay.Models;
using ReadySignOn.ReadyPay.Services;
using SmartStore;
using SmartStore.ComponentModel;
using SmartStore.Web.Framework.Controllers;
using SmartStore.Web.Framework.Security;
using SmartStore.Web.Framework.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace ReadySignOn.ReadyPay.Controllers
{
    public class ReadyPayController : PublicControllerBase
    {
        private readonly IReadyPayService _apiService;

        public ReadyPayController(
            IReadyPayService apiService
        )
        {
            _apiService = apiService;
        }

		protected ActionResult GetActionResult(ReadyPayViewModel model)
		{
			switch (model.Result)
			{
				case ReadyPayResultType.None:
					return new EmptyResult();

				case ReadyPayResultType.PluginView:
					return View(model);

				case ReadyPayResultType.Unauthorized:
					return new HttpUnauthorizedResult();

				case ReadyPayResultType.Redirect:
				default:
					return RedirectToAction(model.RedirectAction, model.RedirectController, new { area = "" });
			}
		}

        [AdminAuthorize, ChildActionOnly, LoadSetting]
        public ActionResult Configure(ReadyPaySettings settings, int storeScope)
		{
			var model = new ReadyPayConfigurationModel();

			MiniMapper.Map(settings, model);
			_apiService.SetupConfiguration(model, storeScope);

			return View(model);
		}

        [HttpPost, AdminAuthorize]
        public ActionResult Configure(ReadyPayConfigurationModel model, FormCollection form)
        {
            var storeDependingSettingHelper = new StoreDependingSettingHelper(ViewData);
            var storeScope = this.GetActiveStoreScopeConfiguration(Services.StoreService, Services.WorkContext);
            var settings = Services.Settings.LoadSetting<ReadyPaySettings>(storeScope);

            if (!ModelState.IsValid)
                return Configure(settings, storeScope);

            ModelState.Clear();

            model.AccessKey = model.AccessKey.TrimSafe();
            model.ClientId = model.ClientId.TrimSafe();
            model.ClientSecret = model.ClientSecret.TrimSafe();
            model.MerchantId = model.MerchantId.TrimSafe();

            MiniMapper.Map(model, settings);

            using (Services.Settings.BeginScope())
            {
                storeDependingSettingHelper.UpdateSettings(settings, form, storeScope, Services.Settings);
            }

            using (Services.Settings.BeginScope())
            {
                Services.Settings.SaveSetting(settings, x => x.UseSandbox, 0, false);
            }

            NotifySuccess(T("Admin.Common.DataSuccessfullySaved"));

            return RedirectToConfiguration(Plugin.SystemName, false);
        }

        // This is the payment plugin page for collecting payment information such as credit card info etc.
        public ActionResult PaymentInfo()
		{
			var model = new ReadyPayPaymentInfoModel();
			model.CurrentPageIsBasket = ControllerContext.ParentActionViewContext.RequestContext.RouteData.IsRouteEqual("ShoppingCart", "Cart");

			if (model.CurrentPageIsBasket)
			{
				var settings = Services.Settings.LoadSetting<ReadyPaySettings>(Services.StoreContext.CurrentStore.Id);

				model.SubmitButtonImageUrl = "~/Plugins/ReadySignOn.ReadyPay/Content/ready_button.png";
			}

			return PartialView(model);
		}

        // This payment plugin method for handling mini shopping card specifically
        public ActionResult MiniShoppingCart()
        {
            var settings = Services.Settings.LoadSetting<ReadyPaySettings>(Services.StoreContext.CurrentStore.Id);

            if (settings.ShowButtonInMiniShoppingCart)
            {
                var model = new ReadyPayPaymentInfoModel();
                model.SubmitButtonImageUrl = "~/Plugins/ReadySignOn.ReadyPay/Content/ready_button.png";

                return PartialView(model);
            }

            return new EmptyResult();
        }

        //GetDisplayWidgetRoute sets the routes so this method will be used for displaying the wedget at various zones.
        public ActionResult InPlaceReadyPay()
        {
            var settings = Services.Settings.LoadSetting<ReadyPaySettings>(Services.StoreContext.CurrentStore.Id);

                var model = new ReadyPayPaymentInfoModel();
                model.SubmitButtonImageUrl = "~/Plugins/ReadySignOn.ReadyPay/Content/ready_button.png";

                return PartialView(model);
        }

    }
}
