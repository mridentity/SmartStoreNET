﻿using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.IO;
using SmartStore;
using SmartStore.Core.Logging;
using SmartStore.Core.Domain.Customers;
using SmartStore.Services.Customers;
using SmartStore.Core.Domain.Orders;
using SmartStore.Core.Domain.Discounts;
using SmartStore.Web.Models.Checkout;
using SmartStore.Core.Domain.Shipping;
using SmartStore.Services.Common;
using System.Net;
using SmartStore.Core.Domain.Common;

namespace ReadySignOn.ReadyPay.Controllers
{
    public partial class ReadyPayController
    {
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> UpdatePaymentMethodCost()
        {
            Logger.Info("UpdatePaymentMethodCost is called");

            StreamReader stream = new StreamReader(Request.InputStream);
            string json_request = stream.ReadToEnd();

            JObject jInput = JObject.Parse(json_request);
            JObject jOutput = new JObject();

            try
            {
                //Get the customer guid encoded in base64 of this payment
                string customer_guid_b64 = jInput.Value<string>("appDataB64");
                if (string.IsNullOrEmpty(customer_guid_b64))
                {
                    throw new ArgumentException("//Customer GUID cannot be null or empty when updating shipping methods for contract.");
                }

                //Get selected payment name
                string selected_payment = jInput.Value<string>("paymentName");
                if(string.IsNullOrEmpty(selected_payment))
                {
                    throw new ArgumentException("//Payment method name cannot be null or empty when updating payment method cost.");
                }

                //Get payment network name
                string payment_network = jInput.Value<string>("paymentNetwork");
                if(string.IsNullOrEmpty(payment_network))
                {
                    throw new ArgumentException("//Payment network cannot be null or empty when updating payment method cost.");
                }

                //Get payment type
                string payment_type = jInput.Value<string>("PaymentType");
                if(string.IsNullOrEmpty(payment_type))
                {
                    throw new ArgumentException("//Payment type cannot be null or empty when updating payment method cost.");
                }

                // TODO: Generate payment method costs

            }
            catch(Exception ex)
            {
                Logger.Error(ex);
                return new EmptyResult();
            }

            return Json(jInput, "application/json", Encoding.UTF8);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> UpdateShippingCostForContact()
        {
            Logger.Info("ShippingMethodsForContact is called");
            StreamReader stream = new StreamReader(Request.InputStream);
            string json_request = stream.ReadToEnd();

            JObject jInput = JObject.Parse(json_request);
            JObject jOutput = new JObject();

            try
            {
                //Get the customer guid encoded in base64 of this payment
                string customer_guid_b64 = jInput.Value<string>("appDataB64");
                if (string.IsNullOrEmpty(customer_guid_b64))
                {
                    throw new ArgumentException("//Customer GUID cannot be null or empty when updating shipping methods for contract.");
                }

                //Get street address
                string street_address = jInput.Value<string>("street");
                if (string.IsNullOrEmpty(street_address))
                {
                    Logger.Warn("//Street address cannot be null or empty when updating shipping methods for contract.");
                }

                //Get cit
                string city = jInput.Value<string>("city");
                if (string.IsNullOrEmpty(city))
                {
                    throw new ArgumentException("//City cannot be null or empty when updating shipping methods for contract.");
                }

                //Get state
                string state = jInput.Value<string>("state");
                if (string.IsNullOrEmpty(state))
                {
                    throw new ArgumentException("//State cannot be null or empty when updating shipping methods for contract.");
                }

                //Get postal code
                string postal_code = jInput.Value<string>("postalCode");
                if (string.IsNullOrEmpty(postal_code))
                {
                    throw new ArgumentException("//PostalCode cannot be null or empty when updating shipping methods for contract.");
                }

                //Get country
                string country = jInput.Value<string>("country");
                if (string.IsNullOrEmpty(country))
                {
                    throw new ArgumentException("//Country cannot be null or empty when updating shipping methods for contract.");
                }

                //Get country code
                string iso_country_code = jInput.Value<string>("isoCountryCode");
                if (string.IsNullOrEmpty(iso_country_code))
                {
                    throw new ArgumentException("//Country code cannot be null or empty when updating shipping methods for contract.");
                }

                // Generate list of shipping options
                var customer = _customerService.GetCustomerByGuid(new Guid(Encoding.UTF8.GetString(Convert.FromBase64String(customer_guid_b64))));
                if (customer == null)
                {
                    throw new ArgumentException("//Cannot locate the requesting customer during the request session.");
                }

                var cart = customer.GetCartItems(ShoppingCartType.ShoppingCart);

                var countryAllowsShipping = true;
                var countryAllowsBilling = true;

                Address shipping_address = CreateAddress(null,
                                                null,
                                                null,
                                                street_address,
                                                null,
                                                null,
                                                city,
                                                postal_code,
                                                null,
                                                iso_country_code,
                                                state,
                                                country,
                                                null,
                                                out countryAllowsShipping,
                                                out countryAllowsBilling);

                customer.ShippingAddress = shipping_address;
                CheckoutShippingMethodModel co_sm = _readyPayService.GetShippingMethodModel(customer, cart);

                JArray j_methods = new JArray();

                foreach (var sm in co_sm.ShippingMethods)
                {
                    JObject j_method = new JObject();
                    j_method["Lbl"] = sm.Name;
                    j_method["Desc"] = sm.ShippingRateComputationMethodSystemName;
                    j_method["Id"] = sm.Name.Replace(" ", string.Empty);
                    j_method["Final"] = true;
                    j_method["Amt"] = sm.FeeRaw.RoundIfEnabledFor(_workContext.WorkingCurrency);
                    if (sm.Selected)
                    {
                        Logger.Info($"Adding {sm.Name} shipping method to first with a {sm.Fee} fee.");
                        j_methods.AddFirst(j_method);
                    }
                    else
                    {
                        Logger.Info($"Adding {sm.Name} shipping method with a {sm.Fee} fee.");
                        j_methods.Add(j_method);
                    }
                }

                jOutput["ShpMthds"] = j_methods;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, ex.Message);
            }

            return Json(jOutput, "application/json", Encoding.UTF8);
        }
    }
}