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
			//_apiService.SetupConfiguration(model);

			return View(model);
		}

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

        public ActionResult InPlaceReadyPay()
        {
            var settings = Services.Settings.LoadSetting<ReadyPaySettings>(Services.StoreContext.CurrentStore.Id);

                var model = new ReadyPayPaymentInfoModel();
                model.SubmitButtonImageUrl = "~/Plugins/ReadySignOn.ReadyPay/Content/ready_button.png";

                return PartialView(model);
        }

    }
}
