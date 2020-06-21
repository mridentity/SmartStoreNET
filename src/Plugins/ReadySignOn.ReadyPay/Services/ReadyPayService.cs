using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReadySignOn.ReadyPay.Models;
using SmartStore;
using SmartStore.Core;
using SmartStore.Core.Domain.Customers;
using SmartStore.Core.Domain.Discounts;
using SmartStore.Core.Domain.Orders;
using SmartStore.Core.Domain.Shipping;
using SmartStore.Core.Domain.Tax;
using SmartStore.Services;
using SmartStore.Services.Common;
using SmartStore.Services.Customers;
using SmartStore.Services.Shipping;
using SmartStore.Services.Tax;
using SmartStore.Web.Models.Checkout;
using System;
using System.IO;
using System.Net;
using System.Text;
using SmartStore.Core.Logging;
using SmartStore.Services.Catalog;
using SmartStore.Services.Directory;
using SmartStore.Services.Orders;
using System.Collections.Generic;
using System.Linq;

namespace ReadySignOn.ReadyPay.Services
{
    public class ReadyPayService : IReadyPayService
    {
        private readonly ICommonServices _services;
        private readonly IAddressService _addressService;
        private readonly IShippingService _shippingService;
        private readonly ITaxService _taxService;
        private readonly IWorkContext _workContext;
        private readonly IPriceFormatter _priceFormatter;
        private readonly ICurrencyService _currencyService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IProductService _productService;
        private readonly IReadyPayOrders _readyPayOrders;
        private readonly ICountryService _countryService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ICustomerService _customerService;
        private readonly TaxSettings _taxSettings;

        public ReadyPayService(
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
                    IShoppingCartService            shoppingCartService,
                    ICustomerService                customerService,
                    IShippingService                shippingService,
                    IAddressService                 addressService,
                    TaxSettings                     taxSettings)
        {
            _addressService = addressService;
            _workContext = workContext;
            _currencyService = currencyService;
            _priceFormatter = priceFormatter;
            _services = services;
            _countryService = countryService;
            _stateProvinceService = stateProvinceService;
            _orderTotalCalculationService = orderTotalCalculationService;
            _productService = productService;
            _taxService = taxService;
            _readyPayOrders = readyPayOrders;
            _shoppingCartService = shoppingCartService;
            _customerService = customerService;
            _shippingService = shippingService;
            _taxSettings = taxSettings;
        }

        /// <summary>
        /// Create a ready payment request and submit it to ReadySignOn CreatePurchaseRequest endpoint
        /// </summary>
        /// <param name="rp_info_model">The model object used to create the ready payment request</param>
        /// <returns>A ReadyPayment object that describes the payment that has be sucessfully processed; null if the payment process fails.</returns>
        public ReadyPayment ProcessReadyPay(ReadyPayPaymentInfoModel rp_info_model)
        {
            var rp_settings = _services.Settings.LoadSetting<ReadyPaySettings>(_services.StoreContext.CurrentStore.Id);
            string ep_create_rp_request = rp_settings.UseSandbox ? "https://readyconnectsvcqa.readysignon.com/api/ReadyPay/CreatePurchaseRequest/"
                                                 : "https://readyconnectsvc.readysignon.com/api/ReadyPay/CreatePurchaseRequest/";
            ep_create_rp_request += rp_info_model.ReadyTicket;

            string url_paymentupdate = _services.StoreContext.CurrentStore.SecureUrl.EnsureEndsWith("/") + "Plugins/ReadySignOn.ReadyPay/ReadyPay/";

            if (url_paymentupdate.StartsWith("https://localhost", StringComparison.OrdinalIgnoreCase))
            {
                //Since localhost is unreachable by other devices we will set the paymentupdate endpoint to
                //the default ReadyConnect paymentupdate endpoint. Note: ReadyConnect endpoint cannot and
                //will not produce the correct updates (shipping methods, costs, tax etc.) that are specific
                //to individual merchants during the processing of a readyPay request, it simply echos whatever
                //it receives back to the caller so that readyPay requests can be processed without error. This
                //endpoint is offered as a convenient for developers to test and debug their implementations.
                //!!! WARNING!!!! LIVE PRODUCTION IMPLEMENTATION SHOULD NOT RELY ON THIS CONVENIENT ENDPOINT
                // AS DOING SO MAY ALLOW ORDER TO BE PLACED WITH INCORRECT SHIPPING CHARGES OR FEES. EACH MERCHANT
                // MUST IMPLEMENT THEIR OWN PAYMENT UPDATE ENDPOINT THAT PROVIDES SHIPPING METHODS AND FEES SPECIFIC
                // TO THEIR BUSINESS.

                url_paymentupdate = rp_settings.UseSandbox  ? "https://readyconnectsvcqa.readysignon.com/PaymentUpdate/"
                                                            : "https://readyconnectsvc.readysignon.com/PaymentUpdate/";
            }

            string payment_processing_country = rp_settings.PaymentProcessingCountryCode;
            if (string.IsNullOrWhiteSpace(payment_processing_country)) payment_processing_country = "US";

            string customer_guid = _services.WorkContext.CurrentCustomer.CustomerGuid.ToString();

            string application_data_b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(customer_guid));

            //https://stackoverflow.com/questions/9145667/how-to-post-json-to-a-server-using-c
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(ep_create_rp_request);
            httpWebRequest.ContentType = "application/x-www-form-urlencoded";
            httpWebRequest.Method = "POST";
            httpWebRequest.Headers.Add("client_id", rp_settings.ClientId);
            httpWebRequest.Headers.Add("client_name", Plugin.SystemName);
            httpWebRequest.Headers.Add("client_secret", rp_settings.ClientSecret);
            httpWebRequest.Headers.Add("sentinel", rp_info_model.Sentinel);

            JObject payment_request = new JObject();
            payment_request["GatewayId"] = rp_settings.GatewayId;
            payment_request["MerchantId"] = rp_settings.MerchantId;
            payment_request["UseSandbox"] = rp_settings.UseSandbox;
            payment_request["AppDataB64"] = application_data_b64;
            payment_request["RpUpdEp"] = url_paymentupdate;
            payment_request["CountryCd"] = payment_processing_country;
            payment_request["CurrencyCd"] = _services.StoreContext.CurrentStore.PrimaryStoreCurrency.CurrencyCode;
            payment_request["RqBilPa"] = true;
            payment_request["RqBilEmail"] = true;
            payment_request["RqBilPh"] = true;
            payment_request["RqShpPa"] = true;
            payment_request["RqShpEmail"] = true;
            payment_request["RqShpPh"] = true;

            payment_request["PmtNwks"] = JArray.FromObject(new string[] {"AmEx","Visa","MasterCard","Discover"}); // TODO: Perhaps need to create setting options for this.

            var cart = _services.WorkContext.CurrentCustomer.GetCartItems(ShoppingCartType.ShoppingCart);
            CheckoutShippingMethodModel sm_mod = GetShippingMethodModel(_services.WorkContext.CurrentCustomer, cart);

            if (sm_mod != null && sm_mod.ShippingMethods.Count > 0)
            {
                JArray j_methods = new JArray();

                foreach (var sm in sm_mod.ShippingMethods)
                {
                    JObject j_method = new JObject();
                    j_method["Lbl"] = sm.Name;
                    j_method["Desc"] = sm.ShippingRateComputationMethodSystemName;
                    j_method["Id"] = sm.Name.Replace(" ", string.Empty);
                    j_method["Final"] = true;
                    j_method["Amt"] = sm.FeeRaw.RoundIfEnabledFor(_workContext.WorkingCurrency);
                    if (sm.Selected)
                    {
                        j_methods.AddFirst(j_method);
                    }
                    else
                    {
                        j_methods.Add(j_method);
                    }
                }

                payment_request["ShpMthds"] = j_methods;
            }


            JArray j_summary_items = new JArray();
                {
                    JObject j_summary_item = new JObject();
                    j_summary_item["Lbl"] = "Cart Price";
                    j_summary_item["Amt"] = rp_info_model.CartSubTotal.RoundIfEnabledFor(_workContext.WorkingCurrency);
                    j_summary_item["Final"] = true;

                    j_summary_items.Add(j_summary_item);
                }

                {
                    JObject j_summary_item = new JObject();
                    j_summary_item["Lbl"] = "Tax";
                    j_summary_item["Amt"] = rp_info_model.TaxTotal.RoundIfEnabledFor(_workContext.WorkingCurrency);
                    j_summary_item["Final"] = true;

                    j_summary_items.Add(j_summary_item);
                }

                {
                    JObject j_summary_item = new JObject();
                    j_summary_item["Lbl"] = "Total";
                    j_summary_item["Amt"] = (rp_info_model.CartSubTotal + rp_info_model.TaxTotal).RoundIfEnabledFor(_workContext.WorkingCurrency);
                    j_summary_item["Final"] = true;

                    j_summary_items.Add(j_summary_item);
                }

            payment_request["SumItems"] = j_summary_items;

            string json_payment_request = payment_request.ToString(Formatting.None);    // The default format is Indented with tabs and spaces; Formatting.None miniturizes the result string.

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                //https://www.codeproject.com/Questions/1230349/Remove-extra-space-in-json-string
                //string clean_json = JsonConvert.DeserializeObject(json).ToString();

                string body = "payment_request_b64=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(json_payment_request));

                streamWriter.Write(body);
            }

            try
            {
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                if (httpResponse.StatusCode == HttpStatusCode.OK)
                {
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        string json_result = streamReader.ReadToEnd();
                        if (string.IsNullOrWhiteSpace(json_result)) {
                            throw new InvalidDataException("Missing payment information.");
                        }

                        if (JObject.Parse(json_result).ContainsKey("paymentData"))    // Apple payment
                        {
                            ReadyPayment pk_payment = JsonConvert.DeserializeObject<ReadyPayment>(json_result);
                            return pk_payment;
                        }
                        else if (JObject.Parse(json_result).ContainsKey("paymentMethodData"))   // Google payment
                        {
                            dynamic android_payment_jobj = JsonConvert.DeserializeObject<dynamic>(json_result);
                            //TODO: Generate a ReadyPayment object based on Google payment information below:
                            //{
                            //  "apiVersionMinor": 0,
                            //  "apiVersion": 2,
                            //  "paymentMethodData": {
                            //    "description": "Visa •••• 6229",
                            //    "type": "CARD",
                            //    "info": {
                            //      "cardNetwork": "VISA",
                            //      "cardDetails": "6229",
                            //      "billingAddress": {
                            //        "address3": "",
                            //        "sortingCode": "",
                            //        "address2": "",
                            //        "countryCode": "US",
                            //        "address1": "111 South Highland Street",
                            //        "postalCode": "12345",
                            //        "name": "Dev Sweech",
                            //        "locality": "New York",
                            //        "administrativeArea": "NY"
                            //      }
                            //    }
                            //  },
                            //  "shippingAddress": {
                            //    "phoneNumber": "+1 804-555-5555",
                            //    "address3": "",
                            //    "sortingCode": "",
                            //    "address2": "",
                            //    "countryCode": "US",
                            //    "address1": "111 South Highland Street",
                            //    "postalCode": "12345",
                            //    "name": "Dev Sweech",
                            //    "locality": "New York",
                            //    "administrativeArea": "NY"
                            //  },
                            //  "email": "devsweech@gmail.com",
                            //  "transactionIdentifier": "TX_ID_RETURNED_FROM_PSP",
                            //  "grandTotalCharged": 1.23
                            //}

                            if (android_payment_jobj != null)
                            {
                                ReadyPayment pk_payment = new ReadyPayment();
                                pk_payment.transactionIdentifier = android_payment_jobj.transactionIdentifier;
                                pk_payment.grandTotalCharged = android_payment_jobj.grandTotalCharged;

                                PaymentMethod paymentMethod = new PaymentMethod();
                                paymentMethod.network = android_payment_jobj.paymentMethodData.info.cardNetwork;
                                paymentMethod.type = android_payment_jobj.paymentMethodData.type;
                                paymentMethod.displayName = android_payment_jobj.paymentMethodData.description;

                                pk_payment.paymentMethod = paymentMethod;

                                PaymentData paymentData = new PaymentData();
                                paymentData.version = android_payment_jobj.apiVersion;

                                pk_payment.paymentData = paymentData;

                                Models.ShippingMethod shippingMethod = new Models.ShippingMethod();
                                shippingMethod.amount = android_payment_jobj.shippingFee; //TODO: need to pass the shipping amount from android to RP via ReadyConnectSvc

                                pk_payment.shippingMethod = shippingMethod;

                                RPContact billingContact = new RPContact();
                                billingContact.emailAddress = android_payment_jobj.email;

                                IEnumerable<string> name_parts = android_payment_jobj.paymentMethodData.info.billingAddress.name.ToString().Split();
                                billingContact.givenName = name_parts.FirstOrDefault();
                                billingContact.familyName = name_parts.LastOrDefault();
                                billingContact.street = android_payment_jobj.paymentMethodData.info.billingAddress.address1;
                                billingContact.city = android_payment_jobj.paymentMethodData.info.billingAddress.locality;
                                billingContact.postalCode = android_payment_jobj.paymentMethodData.info.billingAddress.postalCode;
                                billingContact.isoCountryCode = android_payment_jobj.paymentMethodData.info.billingAddress.countryCode;
                                billingContact.state = android_payment_jobj.paymentMethodData.info.billingAddress.administrativeArea;
                                billingContact.country = android_payment_jobj.paymentMethodData.info.billingAddress.countryCode;

                                pk_payment.billingContact = billingContact;

                                RPContact shippingContact = new RPContact();
                                shippingContact.emailAddress = android_payment_jobj.email;

                                name_parts = android_payment_jobj.shippingAddress.name.ToString().Split();
                                shippingContact.givenName = name_parts.FirstOrDefault();
                                shippingContact.familyName = name_parts.LastOrDefault();
                                shippingContact.street = android_payment_jobj.shippingAddress.address1;
                                shippingContact.city = android_payment_jobj.shippingAddress.locality;
                                shippingContact.postalCode = android_payment_jobj.shippingAddress.postalCode;
                                shippingContact.isoCountryCode = android_payment_jobj.shippingAddress.countryCode;
                                shippingContact.state = android_payment_jobj.shippingAddress.administrativeArea;
                                shippingContact.country = android_payment_jobj.shippingAddress.countryCode;

                                pk_payment.shippingContact = shippingContact;

                                return pk_payment;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return null;
        }

        public CheckoutShippingMethodModel GetShippingMethodModel(Customer customer, List<OrganizedShoppingCartItem> cart)
        {
            CheckoutShippingMethodModel model = new CheckoutShippingMethodModel();
            GetShippingOptionResponse getShippingOptionResponse = new GetShippingOptionResponse();

            // Check to see if the cart contain any shippable item.
            var shipping_request = _shippingService.CreateShippingOptionRequest(cart, customer.ShippingAddress, _services.StoreContext.CurrentStore.Id);
            if (shipping_request.Items == null || shipping_request.Items.Count < 1) // No shippable item in cart
            {
                var so = new ShippingOption {
                    Name = _services.Localization.GetResource("No shipping required"),
                    Description = _services.Localization.GetResource("Admin.System.Warnings.NoShipmentItems"),
                    ShippingMethodId = 0,
                    Rate = 0,
                    ShippingRateComputationMethodSystemName = Plugin.SystemName
                };

                getShippingOptionResponse.ShippingOptions.Add(so);
            }
            else
            {
                getShippingOptionResponse = _shippingService.GetShippingOptions(cart, customer.ShippingAddress, "");
            }

            if (getShippingOptionResponse.Success)
            {
                var shippingMethods = _shippingService.GetAllShippingMethods(null);

                foreach (var shippingOption in getShippingOptionResponse.ShippingOptions)
                {
                    var soModel = new CheckoutShippingMethodModel.ShippingMethodModel
                    {
                        ShippingMethodId = shippingOption.ShippingMethodId,
                        Name = shippingOption.Name,
                        Description = shippingOption.Description,
                        ShippingRateComputationMethodSystemName = shippingOption.ShippingRateComputationMethodSystemName,
                    };

                    // Adjust rate.
                    Discount appliedDiscount = null;
                    var shippingTotal = _orderTotalCalculationService.AdjustShippingRate(shippingOption.Rate, cart, shippingOption, shippingMethods, out appliedDiscount);
                    decimal rateBase = _taxService.GetShippingPrice(shippingTotal, customer);
                    decimal rate = _currencyService.ConvertFromPrimaryStoreCurrency(rateBase, _workContext.WorkingCurrency);
                    soModel.FeeRaw = rate;
                    soModel.Fee = _priceFormatter.FormatShippingPrice(rate, true);

                    model.ShippingMethods.Add(soModel);
                }

                // Find a selected (previously) shipping method.
                var selectedShippingOption = customer.GetAttribute<ShippingOption>(SystemCustomerAttributeNames.SelectedShippingOption);
                if (selectedShippingOption != null)
                {
                    var shippingOptionToSelect = model.ShippingMethods
                        .ToList()
                        .Find(so => !String.IsNullOrEmpty(so.Name) && so.Name.Equals(selectedShippingOption.Name, StringComparison.InvariantCultureIgnoreCase) &&
                        !String.IsNullOrEmpty(so.ShippingRateComputationMethodSystemName) &&
                        so.ShippingRateComputationMethodSystemName.Equals(selectedShippingOption.ShippingRateComputationMethodSystemName, StringComparison.InvariantCultureIgnoreCase));

                    if (shippingOptionToSelect != null)
                    {
                        shippingOptionToSelect.Selected = true;
                    }
                }

                // If no option has been selected, let's do it for the first one.
                if (model.ShippingMethods.Where(so => so.Selected).FirstOrDefault() == null)
                {
                    var shippingOptionToSelect = model.ShippingMethods.FirstOrDefault();
                    if (shippingOptionToSelect != null)
                    {
                        shippingOptionToSelect.Selected = true;
                    }
                }

                return model;
            }
            else
            {
                foreach (var error in getShippingOptionResponse.Errors)
                {
                    model.Warnings.Add(error);
                }

                throw new Exception(model.Warnings.StrJoin(";"));
            }
        }
    }
}
