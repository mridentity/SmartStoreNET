using Newtonsoft.Json;
using ReadySignOn.ReadyPay.Models;
using ReadySignOn.ReadyPay.Services;
using SmartStore;
using SmartStore.ComponentModel;
using SmartStore.Core.Domain.Common;
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
using System.Web.Mvc;

namespace ReadySignOn.ReadyPay.Controllers
{
    public class ReadyPayController : PublicControllerBase
    {
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IReadyPayOrders _readyPayOrders;
        private readonly IReadyPayService _apiService;

        public ReadyPayController(
                    IOrderTotalCalculationService orderTotalCalculationService,
                    IReadyPayOrders readyPayOrders,
                    IReadyPayService apiService)
        {
            _orderTotalCalculationService = orderTotalCalculationService;
            _readyPayOrders = readyPayOrders;
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
            throw new NotImplementedException();

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
                model.CurrentPageIsBasket = true;
                model.Sentinel = "abcdEFGH";
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
        public ActionResult InPlaceReadyPayPosted(ReadyPayPaymentInfoModel readypay_request)
        {
            if (String.IsNullOrWhiteSpace(readypay_request.ReadyTicket))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid ReadyTicket.");
            }

            if (String.IsNullOrWhiteSpace(readypay_request.ProductId))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid ProductId.");
            }

            if (readypay_request.OrderTotal <= 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid OrderTotal.");
            }

            try
            {
                ReadyPayment rpayment = _apiService.ProcessReadyPay(readypay_request);
                //TODO: rpayment contains authorized payment and tx information that can
                // be used to create an order in the SmartStore and/or tracking info to
                // be sent to the end user.

                ReadyOrderRequest order_request = PrepareOrderRequest(rpayment);
                order_request.IsInPlaceReadyPayOrder = true;

                var order_result = _readyPayOrders.PlaceOrder(order_request, new Dictionary<string, string>());

                if (!order_result.Success)
                {
                    string statusDescription = string.Empty;
                    if (order_result.Errors.Any())
                    {
                        statusDescription = string.Join(" ", order_result.Errors);
                    }
                    return new HttpStatusCodeResult(HttpStatusCode.NotFound, statusDescription);
                }

                //https://stackoverflow.com/questions/9777731/mvc-how-to-return-a-string-as-json
                //https://exceptionshub.com/return-a-json-string-explicitly-from-asp-net-webapi-4.html
                //https://stackoverflow.com/questions/2422983/returning-json-object-from-an-asp-net-page
                //https://stackoverflow.com/questions/1428585/how-can-i-exclude-some-public-properties-from-being-serialized-into-a-jsonresult

                var result = new
                {
                    tx_id = rpayment.transactionIdentifier,
                    order_id = order_result.PlacedOrder.Id,
                    charged_total = rpayment.grandTotalCharged,
                    payment_method = rpayment.paymentMethod.displayName,
                    shipping_method = rpayment.shippingMethod.detail,
                    shipping_address = $"{rpayment.shippingContact.givenName} {rpayment.shippingContact.familyName}, {rpayment.shippingContact.street}, {rpayment.shippingContact.city}, {rpayment.shippingContact.state} {rpayment.shippingContact.postalCode}, {rpayment.shippingContact.country}",
                    email_address = rpayment.shippingContact.emailAddress,
                    phone_number = rpayment.shippingContact.phoneNumber
                };

                //Note: Json result returned from this.Json() may contain type name handling info described 
                //here: https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_TypeNameHandling.htm
                //This is because the JsonNetAttribute filter is applied on the SmartController base class.
                //We therefore serialize the result to json string manually to avoid the $type inclusion. 
                string json = JsonConvert.SerializeObject(result);

                return new ContentResult
                {
                    Content = json,
                    ContentType = "application/json",
                    ContentEncoding = Encoding.UTF8
                };
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        private ReadyOrderRequest PrepareOrderRequest(ReadyPayment rpayment)
        {
            var order_request = new ReadyOrderRequest();
            order_request.StoreId = Services.StoreContext.CurrentStore.Id;
            order_request.CustomerId = Services.WorkContext.CurrentCustomer.Id;
            order_request.OrderTotal = rpayment.grandTotalCharged;

            var billing_address = new Address();
            billing_address.FirstName = rpayment.billingContact.givenName;
            billing_address.LastName = rpayment.billingContact.familyName;
            billing_address.Address1 = rpayment.billingContact.street;
            billing_address.City = rpayment.billingContact.city;
            billing_address.StateProvince = new SmartStore.Core.Domain.Directory.StateProvince();
            billing_address.StateProvince.Abbreviation = rpayment.billingContact.state;
            billing_address.StateProvince.Name = rpayment.billingContact.state;
            billing_address.ZipPostalCode = rpayment.billingContact.postalCode;
            billing_address.Country = new SmartStore.Core.Domain.Directory.Country();
            billing_address.Country.Name = rpayment.billingContact.country;
            billing_address.StateProvince.Country = billing_address.Country;
            billing_address.CreatedOnUtc = DateTime.UtcNow;
            billing_address.Email = rpayment.billingContact.emailAddress;

            order_request.BillingAddress = billing_address;

            var shipping_address = new Address();
            shipping_address.FirstName = rpayment.shippingContact.givenName;
            shipping_address.LastName = rpayment.shippingContact.familyName;
            shipping_address.Address1 = rpayment.shippingContact.street;
            shipping_address.City = rpayment.shippingContact.city;
            shipping_address.StateProvince = new SmartStore.Core.Domain.Directory.StateProvince();
            shipping_address.StateProvince.Abbreviation = rpayment.shippingContact.state;
            shipping_address.StateProvince.Name = rpayment.shippingContact.state;
            shipping_address.ZipPostalCode = rpayment.shippingContact.postalCode;
            shipping_address.Country = new SmartStore.Core.Domain.Directory.Country();
            shipping_address.Country.Name = rpayment.shippingContact.country;
            shipping_address.StateProvince.Country = shipping_address.Country;
            shipping_address.CreatedOnUtc = DateTime.UtcNow;
            shipping_address.Email = rpayment.shippingContact.emailAddress;

            order_request.ShippingAddress = shipping_address;
            order_request.ShippingMethod = rpayment.shippingMethod.detail;
            return order_request;
        }

        [HttpPost]
        [AllowAnonymous]
        //[ValidateAntiForgeryToken]
        public ActionResult MiniCartReadyPayPosted(ReadyPayPaymentInfoModel readypay_request)
        {
            if (String.IsNullOrWhiteSpace(readypay_request.ReadyTicket))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid ReadyTicket.");
            }

            if (String.IsNullOrWhiteSpace(readypay_request.ProductId))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid ProductId.");
            }

            if (readypay_request.OrderTotal <= 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid OrderTotal.");
            }

            try
            {
                ReadyPayment rpayment = _apiService.ProcessReadyPay(readypay_request);
                //TODO: rpayment contains authorized payment and tx information that can
                // be used to create an order in the SmartStore and/or tracking info to
                // be sent to the end user.

                ReadyOrderRequest order_request = PrepareOrderRequest(rpayment);
                order_request.IsInPlaceReadyPayOrder = false;

                var order_result = _readyPayOrders.PlaceOrder(order_request, new Dictionary<string, string>());

                if (!order_result.Success)
                {
                    string statusDescription = string.Empty;
                    if (order_result.Errors.Any())
                    {
                        statusDescription = string.Join(" ", order_result.Errors);
                    }
                    return new HttpStatusCodeResult(HttpStatusCode.NotFound, statusDescription);
                }

                //https://stackoverflow.com/questions/9777731/mvc-how-to-return-a-string-as-json
                //https://exceptionshub.com/return-a-json-string-explicitly-from-asp-net-webapi-4.html
                //https://stackoverflow.com/questions/2422983/returning-json-object-from-an-asp-net-page
                //https://stackoverflow.com/questions/1428585/how-can-i-exclude-some-public-properties-from-being-serialized-into-a-jsonresult

                var result = new
                {
                    tx_id = rpayment.transactionIdentifier,
                    order_id = order_result.PlacedOrder.Id,
                    charged_total = rpayment.grandTotalCharged,
                    payment_method = rpayment.paymentMethod.displayName,
                    shipping_method = rpayment.shippingMethod.detail,
                    shipping_address = $"{rpayment.shippingContact.givenName} {rpayment.shippingContact.familyName}, {rpayment.shippingContact.street}, {rpayment.shippingContact.city}, {rpayment.shippingContact.state} {rpayment.shippingContact.postalCode}, {rpayment.shippingContact.country}",
                    email_address = rpayment.shippingContact.emailAddress,
                    phone_number = rpayment.shippingContact.phoneNumber
                };

                return RedirectToAction("Completed", "Checkout", new { area = "" });

                //Note: Json result returned from this.Json() may contain type name handling info described 
                //here: https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_TypeNameHandling.htm
                //This is because the JsonNetAttribute filter is applied on the SmartController base class.
                //We therefore serialize the result to json string manually to avoid the $type inclusion. 
                string json = JsonConvert.SerializeObject(result);

                return new ContentResult
                {
                    Content = json,
                    ContentType = "application/json",
                    ContentEncoding = Encoding.UTF8
                };
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
    }
}