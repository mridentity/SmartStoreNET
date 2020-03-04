using Newtonsoft.Json;
using ReadySignOn.ReadyPay.Models;
using ReadySignOn.ReadyPay.Services;
using SmartStore;
using SmartStore.ComponentModel;
using SmartStore.Core;
using SmartStore.Core.Domain.Common;
using SmartStore.Core.Domain.Customers;
using SmartStore.Core.Domain.Discounts;
using SmartStore.Core.Domain.Orders;
using SmartStore.Core.Domain.Shipping;
using SmartStore.Core.Domain.Tax;
using SmartStore.Core.Logging;
using SmartStore.Services;
using SmartStore.Services.Catalog;
using SmartStore.Services.Common;
using SmartStore.Services.Customers;
using SmartStore.Services.Directory;
using SmartStore.Services.Orders;
using SmartStore.Services.Shipping;
using SmartStore.Services.Tax;
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
    public partial class ReadyPayController : PublicControllerBase
    {
        private readonly IWorkContext                   _workContext;
        private readonly IPriceFormatter                _priceFormatter;
        private readonly ICurrencyService               _currencyService;
        private readonly ICommonServices                _services;
        private readonly IOrderTotalCalculationService  _orderTotalCalculationService;
        private readonly IProductService                _productService;
        private readonly ITaxService                    _taxService;
        private readonly IReadyPayOrders                _readyPayOrders;
        private readonly IReadyPayService               _readyPayService;
        private readonly ICountryService                _countryService;
        private readonly IStateProvinceService          _stateProvinceService;
        private readonly IShoppingCartService           _shoppingCartService;
        private readonly ICustomerService               _customerService;
        private readonly IShippingService               _shippingService;
        private readonly TaxSettings                    _taxSettings;

        public ReadyPayController(
                    IWorkContext                    workContext,
                    ICurrencyService                currencyService, 
                    IPriceFormatter                 priceFormatter, 
                    ICommonServices                 services,
                    ICountryService                 countryService,
                    IStateProvinceService           stateProvinceService,
                    IOrderTotalCalculationService   orderTotalCalculationService,
                    IProductService                 productService,
                    ITaxService                     taxService,
                    IReadyPayOrders                 readyPayOrders,
                    IReadyPayService                readyPayService,
                    IShoppingCartService            shoppingCartService,
                    ICustomerService                customerService,
                    IShippingService                shippingService,
                    TaxSettings taxSettings)
        {
            _workContext =                          workContext;
            _currencyService =                      currencyService;
            _priceFormatter =                       priceFormatter;
            _services =                             services;
            _countryService =                       countryService;
            _stateProvinceService =                 stateProvinceService;
            _orderTotalCalculationService =         orderTotalCalculationService;
            _productService =                       productService;
            _taxService =                           taxService;
            _readyPayOrders =                       readyPayOrders;
            _readyPayService =                      readyPayService;
            _shoppingCartService =                  shoppingCartService;
            _customerService =                      customerService;
            _shippingService =                      shippingService;
            _taxSettings =                          taxSettings;
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

        #region Plugin Configuration UI
        public void SetupConfiguration(ReadyPayConfigurationModel model, int storeScope)
        {
            var store = storeScope == 0
                ? _services.StoreContext.CurrentStore
                : _services.StoreService.GetStoreById(storeScope);

            model.PrimaryStoreCurrencyCode = store.PrimaryStoreCurrency.CurrencyCode;
        }

        [AdminAuthorize, ChildActionOnly, LoadSetting]
        public ActionResult Configure(ReadyPaySettings settings, int storeScope)
        {
            var model = new ReadyPayConfigurationModel();

            MiniMapper.Map(settings, model);    // Populate the ReadyPaySettings with system settings

            SetupConfiguration(model, storeScope);

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

        #endregion

        #region ReadyPay UI and processing
        private void PrepareReadyPaymentInfoModel(ReadyPayPaymentInfoModel rp_payment_info)
        {
            //var settings = Services.Settings.LoadSetting<ReadyPaySettings>(Services.StoreContext.CurrentStore.Id);

            var store = Services.StoreContext.CurrentStore;
            //var customer = Services.WorkContext.CurrentCustomer;
            List<OrganizedShoppingCartItem> cart = Services.WorkContext.CurrentCustomer.GetCartItems(ShoppingCartType.ShoppingCart, store.Id);

            rp_payment_info.SubmitButtonImageUrl = "/Plugins/ReadySignOn.ReadyPay/Content/ready_button.png";
            rp_payment_info.LoaderImageUrl = "/Plugins/ReadySignOn.ReadyPay/Content/loader.gif";

            //Get sub-total and discounts that apply to sub-total
            decimal orderSubTotalDiscountAmountBase = decimal.Zero;
            Discount orderSubTotalAppliedDiscount = null;
            decimal subTotalWithoutDiscountBase = decimal.Zero;
            decimal subTotalWithDiscountBase = decimal.Zero;

            _orderTotalCalculationService.GetShoppingCartSubTotal(cart,
                out orderSubTotalDiscountAmountBase, out orderSubTotalAppliedDiscount, out subTotalWithoutDiscountBase, out subTotalWithDiscountBase);

            rp_payment_info.TaxTotal = _orderTotalCalculationService.GetTaxTotal(cart);
            rp_payment_info.ShippingTotal = rp_payment_info.ShippingTotal ?? (_orderTotalCalculationService.GetShoppingCartShippingTotal(cart) ?? decimal.Zero);
            rp_payment_info.AppData = cart.Select(c => c.Item.ProductId.ToString()).ToArray().StrJoin(",");
            rp_payment_info.CartSubTotal = subTotalWithDiscountBase;
            rp_payment_info.CurrentPageIsBasket = true;
            Money cart_sub_total = new Money(rp_payment_info.CartSubTotal, store.PrimaryStoreCurrency);
            rp_payment_info.Sentinel = cart_sub_total.ToString(true);
        }

        // This action will be called as part of the checkout process including the page after the user clicked on Go to cart on the mini shopping cart page.
        public ActionResult PaymentInfo()
        {
            var rp_payment_info_model = new ReadyPayPaymentInfoModel();
            rp_payment_info_model.CurrentPageIsBasket = ControllerContext.ParentActionViewContext.RequestContext.RouteData.IsRouteEqual("ShoppingCart", "Cart");

            if (rp_payment_info_model.CurrentPageIsBasket)  // For the checkout workflow, we only handle readyPay while we're in the cart stage.
            {
                PrepareReadyPaymentInfoModel(rp_payment_info_model);
                return PartialView(rp_payment_info_model);
            }

            return new EmptyResult();
        }

        // This payment plugin method for handling mini shopping card specifically
        public ActionResult MiniShoppingCart()
        {
            var rp_payment_info_model = new ReadyPayPaymentInfoModel();
            var settings = Services.Settings.LoadSetting<ReadyPaySettings>(Services.StoreContext.CurrentStore.Id);

            if (settings.ShowButtonInMiniShoppingCart)
            {
                PrepareReadyPaymentInfoModel(rp_payment_info_model);
                return PartialView(rp_payment_info_model);
            }

            return new EmptyResult();
        }

        //GetDisplayWidgetRoute sets the routes so this method will be used for displaying the wedget at various zones.
        public ActionResult InPlaceReadyPay(string product_id, string product_sku, string widget_zone, decimal product_price)
        {
            if (product_price <= 0)
                return new EmptyResult();

            var product = _productService.GetProductBySku(product_sku);
            if (product == null || product.HasUserAgreement || product.IsGiftCard || product.IsRecurring || product.DisableBuyButton || product.ProductVariantAttributes.Count > 0)
                return new EmptyResult();

            if (widget_zone == "productbox_add_info" && (product.Id.ToString() != product_id    // This is a grouped product 
                                                        || product.ProductType != SmartStore.Core.Domain.Catalog.ProductType.SimpleProduct
                                                        || product.HasTierPrices)
                )
                return new EmptyResult();

            var settings = Services.Settings.LoadSetting<ReadyPaySettings>(Services.StoreContext.CurrentStore.Id);
            
            var rp_payment_info = new ReadyPayPaymentInfoModel();
            rp_payment_info.SubmitButtonImageUrl = "/Plugins/ReadySignOn.ReadyPay/Content/ready_button.png";
            rp_payment_info.LoaderImageUrl = "/Plugins/ReadySignOn.ReadyPay/Content/loader.gif";
            rp_payment_info.AppData = product_id;
            rp_payment_info.CartSubTotal = product_price;

            Money cart_sub_total = new Money(rp_payment_info.CartSubTotal, Services.StoreContext.CurrentStore.PrimaryStoreCurrency);
            rp_payment_info.Sentinel = cart_sub_total.ToString(true);

            var tax_rate = _taxService.GetTaxRate(product.TaxCategoryId, Services.WorkContext.CurrentCustomer);
            // The tax rate is the percentage number so it needs to be divided by 100 to get the actual fraction to be used for calculation.
            rp_payment_info.TaxTotal = product_price * tax_rate / 100;
            // Next we need to figure out shipping total and add shipping tax if any to the tax total
            // However since we're using readyPay we will let readyPay payment update service to handle that.
            return PartialView(rp_payment_info);
        }

        /// <summary>
        /// This action method is used for processing postback from InPlaceReadyPay widget
        /// </summary>
        /// <param name="rp_info_model">readyPay payment model containning informaiton necessary to create a readyPay request.</param>
        /// <returns>Stringified JSON object that contains the necessary information for the front-end script to create a pop-up notification window.</returns>
        [HttpPost]
        [AllowAnonymous]
        public ActionResult InPlaceReadyPayPosted(ReadyPayPaymentInfoModel rp_info_model)
        {
            if (String.IsNullOrWhiteSpace(rp_info_model.ReadyTicket))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid ReadyTicket.");
            }

            if (String.IsNullOrWhiteSpace(rp_info_model.AppData))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid ProductId.");
            }

            if (rp_info_model.CartSubTotal <= 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid CartSubTotal.");
            }

            try
            {
                var store = Services.StoreContext.CurrentStore;
                var customer = Services.WorkContext.CurrentCustomer;
                var cart = customer.GetCartItems(ShoppingCartType.ShoppingCart, store.Id);
                // Clear shopping cart
                cart.ToList().ForEach(sci => _shoppingCartService.DeleteShoppingCartItem(sci.Item, false));

                var addToCartContext = new AddToCartContext
                {
                    Product = _productService.GetProductById(rp_info_model.AppData.ToInt()),
                    CartType = ShoppingCartType.ShoppingCart,
                    Quantity = 1,
                    AddRequiredProducts = false
                };

                _shoppingCartService.AddToCart(addToCartContext);

                if (addToCartContext.Warnings.Count > 0)
                {
                    Logger.Warn(string.Join(" ", addToCartContext.Warnings));
                    // cannot be added to the cart
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid CartSubTotal.");
                }

                cart = customer.GetCartItems(ShoppingCartType.ShoppingCart, store.Id);
                rp_info_model.TaxTotal = _orderTotalCalculationService.GetTaxTotal(cart);
                rp_info_model.ShippingTotal = _orderTotalCalculationService.GetShoppingCartShippingTotal(cart);
                rp_info_model.ShippingTax = _taxService.GetShippingPrice(rp_info_model.CartSubTotal, true, customer) - _taxService.GetShippingPrice(rp_info_model.CartSubTotal, true, customer);


                ReadyPayment rpayment = _readyPayService.ProcessReadyPay(rp_info_model);
                //TODO: rpayment contains authorized payment and tx information that can
                // be used to create an order in the SmartStore and/or tracking info to
                // be sent to the end user.

                ReadyOrderRequest order_request = PrepareOrderRequest(rp_info_model, rpayment);

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

                // Build the json to return to front end script
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

        /// <summary>
        /// This action method is used for handling postbacks from both mini shopping cart as well as the shopping cart (PaymentInfo)
        /// </summary>
        /// <param name="rp_info_model">readyPay payment model containning informaiton necessary to create a readyPay request.</param>
        /// <returns>An HTTP redirect to the order page.</returns>
        [HttpPost]
        [AllowAnonymous]
        public ActionResult MiniCartReadyPayPosted(ReadyPayPaymentInfoModel rp_info_model)
        {
            if (String.IsNullOrWhiteSpace(rp_info_model.ReadyTicket))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid ReadyTicket.");
            }

            if (String.IsNullOrWhiteSpace(rp_info_model.AppData))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid ProductId.");
            }

            if (rp_info_model.CartSubTotal <= 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid CartSubTotal.");
            }

            // Because the tax information is not displayed on the mini cart view the client side would not be able to update it (after a spinned quatity change) without making a call to the server so we refresh our model object it here.
            var store = Services.StoreContext.CurrentStore;
            var customer = Services.WorkContext.CurrentCustomer;
            var cart = customer.GetCartItems(ShoppingCartType.ShoppingCart, store.Id);
            rp_info_model.TaxTotal = _orderTotalCalculationService.GetTaxTotal(cart);
            rp_info_model.ShippingTotal = _orderTotalCalculationService.GetShoppingCartShippingTotal(cart) ?? 0;
            rp_info_model.ShippingTax = _taxService.GetShippingPrice(rp_info_model.ShippingTotal.Value, true, customer) - _taxService.GetShippingPrice(rp_info_model.ShippingTotal.Value, false, customer);

            try
            {
                ReadyPayment rpayment = _readyPayService.ProcessReadyPay(rp_info_model);
                //TODO: rpayment contains authorized payment and tx information that can
                // be used to create an order in the SmartStore and/or tracking info to
                // be sent to the end user.

                ReadyOrderRequest order_request = PrepareOrderRequest(rp_info_model, rpayment);
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
                return RedirectToAction("Completed", "Checkout", new { area = "" });
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        private ReadyOrderRequest PrepareOrderRequest(ReadyPayPaymentInfoModel readypay_request, ReadyPayment rpayment)
        {
            var countryAllowsShipping = true;
            var countryAllowsBilling = true;

            var order_request = new ReadyOrderRequest();
            order_request.StoreId = Services.StoreContext.CurrentStore.Id;
            order_request.CustomerId = Services.WorkContext.CurrentCustomer.Id;

            var billing_address = CreateAddress(rpayment.billingContact.emailAddress ?? rpayment.shippingContact.emailAddress,
                                            rpayment.billingContact.givenName,
                                            rpayment.billingContact.familyName,
                                            rpayment.billingContact.street,
                                            null,
                                            null,
                                            rpayment.billingContact.city,
                                            rpayment.billingContact.postalCode,
                                            rpayment.billingContact.phoneNumber,
                                            rpayment.billingContact.isoCountryCode ?? "US",
                                            rpayment.billingContact.state,
                                            rpayment.billingContact.country,
                                            null,
                                            out countryAllowsShipping,
                                            out countryAllowsBilling);

            order_request.BillingAddress = billing_address;

            var shipping_address = CreateAddress(rpayment.billingContact.emailAddress ?? rpayment.shippingContact.emailAddress,
                                            rpayment.shippingContact.givenName,
                                            rpayment.shippingContact.familyName,
                                            rpayment.shippingContact.street,
                                            null,
                                            null,
                                            rpayment.shippingContact.city,
                                            rpayment.shippingContact.postalCode,
                                            rpayment.shippingContact.phoneNumber,
                                            rpayment.shippingContact.isoCountryCode ?? "US",
                                            rpayment.shippingContact.state,
                                            rpayment.shippingContact.country,
                                            null,
                                            out countryAllowsShipping,
                                            out countryAllowsBilling);

            order_request.ShippingAddress = shipping_address;
            order_request.ShippingMethod = rpayment.shippingMethod.identifier + ":" + rpayment.shippingMethod.detail;
            order_request.IsShippingMethodSet = true;
            order_request.OrderShippingInclTax = rpayment.shippingMethod.amount;
            order_request.OrderShippingExclTax = rpayment.grandTotalCharged - readypay_request.TaxTotal - readypay_request.CartSubTotal;
            order_request.ShippingRateComputationMethodSystemName = Plugin.SystemName;
            order_request.OrderTotal = rpayment.grandTotalCharged;
            order_request.OrderSubtotalExclTax = readypay_request.CartSubTotal;
            order_request.OrderSubtotalInclTax = (order_request.OrderTotal - order_request.OrderShippingInclTax);
            order_request.OrderTax = readypay_request.TaxTotal;
            order_request.OrderTotalRounding = readypay_request.TaxTotal.RoundToNearest(Services.StoreContext.CurrentStore.PrimaryStoreCurrency.RoundOrderTotalDenominator, true);
            order_request.ShoppingCartItemProdutIds = readypay_request.AppData.ToIntArray(); // TODO: Make sure that SHA256(AppData) is identical to rpayment.paymentData.header.applicationData
            order_request.TaxRate = order_request.OrderTax / readypay_request.CartSubTotal * 100.0M;
            return order_request;
        }

        #endregion
        private Address CreateAddress(
            string email,
            string firstName,
            string lastName,
            string addressLine1,
            string addressLine2,
            string addressLine3,
            string city,
            string postalCode,
            string phone,
            string countryCode,
            string stateRegion,
            string county,
            string destrict,
            out bool countryAllowsShipping,
            out bool countryAllowsBilling)
        {
            countryAllowsShipping = countryAllowsBilling = true;

            var address = new Address();
            address.CreatedOnUtc = DateTime.UtcNow;
            address.Email = email;
            address.FirstName = firstName;
            address.LastName = lastName;
            address.Address1 = addressLine1.EmptyNull().Trim().Truncate(4000);
            address.Address2 = addressLine2.EmptyNull().Trim().Truncate(4000);
            address.Address2 = address.Address2.Grow(addressLine3.EmptyNull().Trim(), ", ").Truncate(4000);
            address.City = city.EmptyNull().Trim().Truncate(4000);
            address.ZipPostalCode = postalCode.EmptyNull().Trim().Truncate(4000);
            address.PhoneNumber = phone.EmptyNull().Trim().Truncate(4000);

            if (countryCode.HasValue())
            {
                var country = _countryService.GetCountryByTwoOrThreeLetterIsoCode(countryCode);
                if (country != null)
                {
                    address.CountryId = country.Id;
                    countryAllowsShipping = country.AllowsShipping;
                    countryAllowsBilling = country.AllowsBilling;
                }
            }

            if (stateRegion.HasValue())
            {
                var stateProvince = _stateProvinceService.GetStateProvinceByAbbreviation(stateRegion);
                if (stateProvince != null)
                {
                    address.StateProvinceId = stateProvince.Id;
                }
            }

            // Normalize.
            if (address.Address1.IsEmpty() && address.Address2.HasValue())
            {
                address.Address1 = address.Address2;
                address.Address2 = null;
            }
            else if (address.Address1.HasValue() && address.Address1 == address.Address2)
            {
                address.Address2 = null;
            }

            if (address.CountryId == 0)
            {
                address.CountryId = null;
            }

            if (address.StateProvinceId == 0)
            {
                address.StateProvinceId = null;
            }

            return address;
        }
    }
}