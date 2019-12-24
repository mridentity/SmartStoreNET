using ReadySignOn.ReadyPay.Models;
using ReadySignOn.ReadyPay.Services;
using SmartStore;
using SmartStore.ComponentModel;
using SmartStore.Core.Domain.Discounts;
using SmartStore.Core.Domain.Orders;
using SmartStore.Services.Customers;
using SmartStore.Services.Orders;
using SmartStore.Web.Framework.Controllers;
using SmartStore.Web.Framework.Security;
using SmartStore.Web.Framework.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace ReadySignOn.ReadyPay.Controllers
{
    public class ReadyPayController : PublicControllerBase
    {
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IReadyPayService _apiService;

        public ReadyPayController(
                    IOrderTotalCalculationService orderTotalCalculationService,
                    IReadyPayService apiService)
        {
            _orderTotalCalculationService = orderTotalCalculationService;
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
                var store = Services.StoreContext.CurrentStore;
                var customer = Services.WorkContext.CurrentCustomer;
                var cart = Services.WorkContext.CurrentCustomer.GetCartItems(ShoppingCartType.ShoppingCart, store.Id);

                var model = new ReadyPayPaymentInfoModel();
                model.SubmitButtonImageUrl = "~/Plugins/ReadySignOn.ReadyPay/Content/ready_button.png";
                model.LoaderImageUrl = "~/Plugins/ReadySignOn.ReadyPay/Content/loader.gif";

                //Get sub-total and discounts that apply to sub-total
                decimal orderSubTotalDiscountAmountBase = decimal.Zero;
                Discount orderSubTotalAppliedDiscount = null;
                decimal subTotalWithoutDiscountBase = decimal.Zero;
                decimal subTotalWithDiscountBase = decimal.Zero;

                _orderTotalCalculationService.GetShoppingCartSubTotal(cart,
                    out orderSubTotalDiscountAmountBase, out orderSubTotalAppliedDiscount, out subTotalWithoutDiscountBase, out subTotalWithDiscountBase);

                model.ProductId = "multiple_products_in_shopping_cart";
                model.OrderTotal = subTotalWithDiscountBase;
                return PartialView(model);
            }

            return new EmptyResult();
        }

        //GetDisplayWidgetRoute sets the routes so this method will be used for displaying the wedget at various zones.
        public ActionResult InPlaceReadyPay(string product_id, string product_sku, string product_name, decimal product_price)
        {
            if (product_price <= 0)
                return new EmptyResult();

            var settings = Services.Settings.LoadSetting<ReadyPaySettings>(Services.StoreContext.CurrentStore.Id);

            var model = new ReadyPayPaymentInfoModel();
            model.SubmitButtonImageUrl = "~/Plugins/ReadySignOn.ReadyPay/Content/ready_button.png";
            model.LoaderImageUrl = "~/Plugins/ReadySignOn.ReadyPay/Content/loader.gif";
            model.ProductId = product_id;
            model.OrderTotal = product_price;
            return PartialView(model);
        }

        [HttpPost]
        [AllowAnonymous]
        //[ValidateAntiForgeryToken]
        public ActionResult ReadyRequestPosted(ReadyPayPaymentInfoModel readypay_request)
        {
            try
            {
                ReadyPayment rpayment = _apiService.ProcessReadyPay(readypay_request);
                //TODO: rpayment contains authorized payment and tx information that can
                // be used to create an order in the SmartStore and/or tracking info to
                // be sent to the end user.
                return new HttpStatusCodeResult(HttpStatusCode.Accepted);
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
    }
}