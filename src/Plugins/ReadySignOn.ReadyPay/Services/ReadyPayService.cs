using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReadySignOn.ReadyPay.Models;
using SmartStore;
using SmartStore.Core.Domain.Shipping;
using SmartStore.Core.Domain.Tax;
using SmartStore.Services;
using SmartStore.Services.Common;
using SmartStore.Services.Shipping;
using SmartStore.Services.Tax;
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
        private readonly ITaxService _taxService;
        private readonly TaxSettings _taxSettings;

        public ReadyPayService(
            IAddressService addressService,
            IShippingService shippingService,
            ITaxService taxService,
            TaxSettings taxSettings,
            ICommonServices services)
        {
            _addressService = addressService;
            _shippingService = shippingService;
            _taxService = taxService;
            _taxSettings = taxSettings;
            _services = services;
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

            string url_paymentupdate = rp_settings.UseSandbox  ? "https://iosiapqa.readysignon.com/PaymentUpdate/" 
                                                            : "https://iosiap.readysignon.com/PaymentUpdate/";

            var shippingSettings = _services.Settings.LoadSetting<ShippingSettings>(_services.StoreContext.CurrentStore.Id);

            var org_shipping_address = _addressService.GetAddressById(shippingSettings.ShippingOriginAddressId);
            string payment_processing_country = org_shipping_address != null ? org_shipping_address.Country.TwoLetterIsoCode : "US";   
            //TODO: In the above we assumed the country where the payment will be processed is the same as the originating country of the shipment. Ideally we could add a setting on the readyPay configuration page.

            string application_data_b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(rp_info_model.ProductId));

            //https://stackoverflow.com/questions/9145667/how-to-post-json-to-a-server-using-c
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(ep_create_rp_request);
            httpWebRequest.ContentType = "application/x-www-form-urlencoded";
            httpWebRequest.Method = "POST";
            httpWebRequest.Headers.Add("client_id", rp_settings.ClientId);
            httpWebRequest.Headers.Add("client_name", Plugin.SystemName);
            httpWebRequest.Headers.Add("client_secret", rp_settings.ClientSecret);
            httpWebRequest.Headers.Add("sentinel", rp_info_model.Sentinel);

            JObject payment_request = new JObject();
            payment_request["MerchantIdentifier"] = rp_settings.MerchantId;
            payment_request["ApplicationDataBase64"] = application_data_b64;
            payment_request["ReadyPayUpdateUrl"] = url_paymentupdate;
            payment_request["CountryCode"] = payment_processing_country;
            payment_request["CurrencyCode"] = _services.StoreContext.CurrentStore.PrimaryStoreCurrency.CurrencyCode;
            payment_request["RequireBillingPostalAddress"] = true;
            payment_request["RequireBillingEmailAddress"] = true;
            payment_request["RequireBillingPhoneNumber"] = true;
            payment_request["RequireShippingPostalAddress"] = true;
            payment_request["RequireShippingEmailAddress"] = true;
            payment_request["RequireShippingPhoneNumber"] = true;

            payment_request["SupportedNetworks"] = JArray.FromObject(new string[] {"American Express","Visa","MasterCard","Discover"}); // TODO: Perhaps need to create setting options for this.

            var shipping_methods = _shippingService.GetAllShippingMethods();
            if (shipping_methods != null && shipping_methods.Count > 0)
            {
                JArray j_methods = new JArray();

                foreach (SmartStore.Core.Domain.Shipping.ShippingMethod s_method in shipping_methods)
                {
                    double shipping_cost = 1.23;    //TODO: The actual shipping cost needs to be updated via readyPay PaymentUpdate callback endpoint.

                    JObject j_method = new JObject();
                    j_method["Label"] = s_method.Name;
                    j_method["Detail"] = new Money(shipping_cost, _services.StoreContext.CurrentStore.PrimaryStoreCurrency).ToString(true);
                    j_method["Identifier"] = s_method.Name.Replace(" ", string.Empty);
                    j_method["IsFinal"] = true;
                    j_method["Amount"] = shipping_cost;      
                    j_methods.Add(j_method);
                }

                payment_request["ShippingMethods"] = j_methods;
            }


            //JArray j_shipping_methods = new JArray();
            //    {
            //        JObject j_shipping_method = new JObject();
            //        j_shipping_method["Label"] = "USPS";
            //        j_shipping_method["Detail"] = "United States Postal Services";
            //        j_shipping_method["Identifier"] = "id_usps";
            //        j_shipping_method["Amount"] = 1.23;
            //        j_shipping_method["IsFinal"] = true;
            //        j_shipping_methods.Add(j_shipping_method);

            //    }

            //    {
            //        JObject j_shipping_method = new JObject();
            //        j_shipping_method["Label"] = "UPS";
            //        j_shipping_method["Detail"] = "United Parcel Services";
            //        j_shipping_method["Identifier"] = "id_ups";
            //        j_shipping_method["Amount"] = 4.56;
            //        j_shipping_method["IsFinal"] = true;

            //        j_shipping_methods.Add(j_shipping_method);
            //    }

            //    {
            //        JObject j_shipping_method = new JObject();
            //        j_shipping_method["Label"] = "Fedex";
            //        j_shipping_method["Detail"] = "Federal Express";
            //        j_shipping_method["Identifier"] = "id_fedex";
            //        j_shipping_method["Amount"] = 7.89;
            //        j_shipping_method["IsFinal"] = true;

            //        j_shipping_methods.Add(j_shipping_method);
            //    }

            //payment_request["ShippingMethods"] = j_shipping_methods;

            JArray j_summary_items = new JArray();
                {
                    JObject j_summary_item = new JObject();
                    j_summary_item["Label"] = "Cart Price";
                    j_summary_item["Amount"] = rp_info_model.CartSubTotal;
                    j_summary_item["IsFinal"] = true;

                    j_summary_items.Add(j_summary_item);
                }

                {
                    JObject j_summary_item = new JObject();
                    j_summary_item["Label"] = "Tax";
                    j_summary_item["Amount"] = rp_info_model.TaxTotal;
                    j_summary_item["IsFinal"] = true;

                    j_summary_items.Add(j_summary_item);
                }

                {
                    JObject j_summary_item = new JObject();
                    j_summary_item["Label"] = "Total";
                    j_summary_item["Amount"] = rp_info_model.CartSubTotal + rp_info_model.TaxTotal;
                    j_summary_item["IsFinal"] = true;

                    j_summary_items.Add(j_summary_item);
                }

            payment_request["SummaryItems"] = j_summary_items;

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
