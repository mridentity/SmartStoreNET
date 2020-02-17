using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReadySignOn.ReadyPay.Controllers;
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
using System.Web.Mvc;
using System.Web.Routing;

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

            url_paymentupdate = _services.StoreContext.CurrentStore.Url.EnsureEndsWith("/") + "Plugins/ReadySignOn.ReadyPay/ReadyPay/";

            var shippingSettings = _services.Settings.LoadSetting<ShippingSettings>(_services.StoreContext.CurrentStore.Id);

            var org_shipping_address = _addressService.GetAddressById(shippingSettings.ShippingOriginAddressId);
            string payment_processing_country = org_shipping_address != null ? org_shipping_address.Country.TwoLetterIsoCode : "US";   
            //TODO: In the above we assumed the country where the payment will be processed is the same as the originating country of the shipment. Ideally we could add a setting on the readyPay configuration page.

            string application_data_b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(rp_info_model.AppData));

            //https://stackoverflow.com/questions/9145667/how-to-post-json-to-a-server-using-c
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(ep_create_rp_request);
            httpWebRequest.ContentType = "application/x-www-form-urlencoded";
            httpWebRequest.Method = "POST";
            httpWebRequest.Headers.Add("client_id", rp_settings.ClientId);
            httpWebRequest.Headers.Add("client_name", Plugin.SystemName);
            httpWebRequest.Headers.Add("client_secret", rp_settings.ClientSecret);
            httpWebRequest.Headers.Add("sentinel", rp_info_model.Sentinel);

            JObject payment_request = new JObject();
            payment_request["MerchantId"] = rp_settings.MerchantId;
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

            var shipping_methods = _shippingService.GetAllShippingMethods();
            if (shipping_methods != null && shipping_methods.Count > 0)
            {
                JArray j_methods = new JArray();

                foreach (SmartStore.Core.Domain.Shipping.ShippingMethod s_method in shipping_methods)
                {
                    double shipping_cost = 1.23;    //TODO: The actual shipping cost needs to be updated via readyPay PaymentUpdate callback endpoint.

                    JObject j_method = new JObject();
                    j_method["Lbl"] = s_method.Name;
                    j_method["Desc"] = new Money(shipping_cost, _services.StoreContext.CurrentStore.PrimaryStoreCurrency).ToString(true);
                    j_method["Id"] = s_method.Name.Replace(" ", string.Empty);
                    j_method["Final"] = true;
                    j_method["Amt"] = shipping_cost;      
                    j_methods.Add(j_method);
                }

                payment_request["ShpMthds"] = j_methods;
            }


            JArray j_summary_items = new JArray();
                {
                    JObject j_summary_item = new JObject();
                    j_summary_item["Lbl"] = "Cart Price";
                    j_summary_item["Amt"] = rp_info_model.CartSubTotal;
                    j_summary_item["Final"] = true;

                    j_summary_items.Add(j_summary_item);
                }

                {
                    JObject j_summary_item = new JObject();
                    j_summary_item["Lbl"] = "Tax";
                    j_summary_item["Amt"] = rp_info_model.TaxTotal;
                    j_summary_item["Final"] = true;

                    j_summary_items.Add(j_summary_item);
                }

                {
                    JObject j_summary_item = new JObject();
                    j_summary_item["Lbl"] = "Total";
                    j_summary_item["Amt"] = rp_info_model.CartSubTotal + rp_info_model.TaxTotal;
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
