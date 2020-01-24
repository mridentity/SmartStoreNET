﻿using Newtonsoft.Json;
using ReadySignOn.ReadyPay.Models;
using SmartStore.Core.Domain.Shipping;
using SmartStore.Services;
using SmartStore.Services.Common;
using SmartStore.Services.Shipping;
using System;
using System.IO;
using System.Net;
using System.Text;

namespace ReadySignOn.ReadyPay.Services
{
    public class ReadyPayService : IReadyPayService
    {
        private readonly ICommonServices _services;
        private readonly IAddressService _addressService;
        private readonly IShippingService _shippingService;

        public ReadyPayService(
            IAddressService addressService,
            IShippingService shippingService,
            ICommonServices services)
        {
            _addressService = addressService;
            _shippingService = shippingService;
            _services = services;
        }

        public ReadyPayment ProcessReadyPay(ReadyPayPaymentInfoModel rp_info_model)
        {
            var settings = _services.Settings.LoadSetting<ReadyPaySettings>(_services.StoreContext.CurrentStore.Id);
            string ep_create_rp_request = settings.UseSandbox ? "https://readyconnectsvcqa.readysignon.com/api/ReadyPay/CreatePurchaseRequest/"
                                                 : "https://readyconnectsvc.readysignon.com/api/ReadyPay/CreatePurchaseRequest/";

            var shippingSettings = _services.Settings.LoadSetting<ShippingSettings>(_services.StoreContext.CurrentStore.Id);

            var org_shipping_address = _addressService.GetAddressById(shippingSettings.ShippingOriginAddressId);
            string two_letter_billing_country_code = org_shipping_address != null ? org_shipping_address.Country.TwoLetterIsoCode : "US";

            var shipping_methods = _shippingService.GetAllShippingMethods();

            foreach (SmartStore.Core.Domain.Shipping.ShippingMethod s_method in shipping_methods)
            {
                // TODO: generate json for enabled shipping methods, then insert it into the json payment request later.
            }

            string application_data_b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(rp_info_model.ProductId));

            string url_paymentupdate = settings.UseSandbox  ? "https://iosiapqa.readysignon.com/PaymentUpdate/" 
                                                            : "https://iosiap.readysignon.com/PaymentUpdate/";
            ep_create_rp_request += rp_info_model.ReadyTicket;

            //https://stackoverflow.com/questions/9145667/how-to-post-json-to-a-server-using-c
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(ep_create_rp_request);
            httpWebRequest.ContentType = "application/x-www-form-urlencoded";
            httpWebRequest.Method = "POST";
            httpWebRequest.Headers.Add("client_id", settings.ClientId);
            httpWebRequest.Headers.Add("client_name", Plugin.SystemName);
            httpWebRequest.Headers.Add("client_secret", settings.ClientSecret);
            httpWebRequest.Headers.Add("sentinel", rp_info_model.Sentinel);

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = 
                         "{\"MerchantIdentifier\":\"" + settings.MerchantId + "\"," +  // ReadyPay merchant id to be used for a particular PSP (e.g. merchant.com.adyen.readypay.test) which is registered at the PSP and the RSO app manifest.
                          "\"ApplicationDataBase64\":\"" + application_data_b64 + "\"," +   // Application data that is to be carried and preserved by ReadyPay as it which can be used to validate or match a specific tx.
                          "\"ReadyPayUpdateUrl\":\"" + url_paymentupdate + "\"," +
                          "\"CountryCode\":\"" + two_letter_billing_country_code + "\" ," +   // We assume the apple pay payment will be process in the country where the shipment will be originated.
                          "\"CurrencyCode\":\"" + _services.StoreContext.CurrentStore.PrimaryStoreCurrency.CurrencyCode + "\" ," +
                          "\"RequireBillingPostalAddress\":true," +
                          "\"RequireBillingEmailAddress\":true," +
                          "\"RequireBillingPhoneNumber\":true," +
                          "\"RequireShippingPostalAddress\":true," +
                          "\"RequireShippingEmailAddress\":true," +
                          "\"RequireShippingPhoneNumber\":true," +
                          "\"SupportedNetworks\" : [" +
                              "\"American Express\"," +
                              "\"Visa\"," +
                              "\"MasterCard\"," +
                              "\"Discover\"" +
                          "]," +
                          "\"ShippingMethods\" : [" +
                              "{\"Label\": \"USPS\"," +
                                  "\"Detail\": \"United States Postal Services\"," +
                                  "\"Identifier\": \"id_usps\", " +
                                  "\"Amount\": 1.23, " +
                                  "\"IsFinal\": true}," +
                              "{\"Label\": \"UPS\"," +
                                  "\"Detail\": \"United Parcel Services\"," +
                                  "\"Identifier\": \"id_ups\", " +
                                  "\"Amount\": 4.56, " +
                                  "\"IsFinal\": true}," +
                              "{\"Label\": \"Fedex\"," +
                              "\"Detail\": \"Federal Express\"," +
                              "\"Identifier\": \"id_fedex\", " +
                              "\"Amount\": 7.89, " +
                              "\"IsFinal\": true}" +
                          "]," +
                          "\"SummaryItems\" : [" +
                              "{\"Label\": \"Cart Price\", \"Amount\": " + rp_info_model.CartSubTotal.ToString() + ", \"IsFinal\": true}," +
                              "{\"Label\": \"Tax\", \"Amount\": " + rp_info_model.TaxTotal.ToString() + ", \"IsFinal\": true}," +
                              "{\"Label\": \"Total\", \"Amount\":" + (rp_info_model.CartSubTotal + rp_info_model.TaxTotal).ToString() + ", \"IsFinal\": true}" +
                          "]" +
                       "}";

                //https://www.codeproject.com/Questions/1230349/Remove-extra-space-in-json-string
                //string clean_json = JsonConvert.DeserializeObject(json).ToString();

                string body = "payment_request_b64=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

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
                        ReadyPayment pk_payment = JsonConvert.DeserializeObject<ReadyPayment>(json_result);

                        return pk_payment;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return null;
        }
    }
}
