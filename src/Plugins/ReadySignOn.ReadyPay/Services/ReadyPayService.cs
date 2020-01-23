using Newtonsoft.Json;
using ReadySignOn.ReadyPay.Models;
using SmartStore.Services;
using System;
using System.IO;
using System.Net;
using System.Text;

namespace ReadySignOn.ReadyPay.Services
{
    public class ReadyPayService : IReadyPayService
    {
        private readonly ICommonServices _services;

        public ReadyPayService(
            ICommonServices services)
        {
            _services = services;
        }

        public ReadyPayment ProcessReadyPay(ReadyPayPaymentInfoModel rp_info_model)
        {
            var settings = _services.Settings.LoadSetting<ReadyPaySettings>(_services.StoreContext.CurrentStore.Id);
            string ep_create_rp_request = settings.UseSandbox ? "https://readyconnectsvcqa.readysignon.com/api/ReadyPay/CreatePurchaseRequest/"
                                                 : "https://readyconnectsvc.readysignon.com/api/ReadyPay/CreatePurchaseRequest/";

            string application_data_b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(rp_info_model.ProductId));


            string items_json = "\"SummaryItems\" : [";

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
            httpWebRequest.Headers.Add("sentinel", string.Format("${0:C}", rp_info_model.OrderTotal));

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                decimal? order_tax = rp_info_model.OrderTotal * Plugin.FlatPercentTaxRate;

                string json = "{\"user\":\"test\"," +
                              "\"password\":\"bla\"}";

                json = "{\"MerchantIdentifier\":\"" + settings.MerchantId + "\"," +         // ReadyPay merchant id to be used for a particular PSP (e.g. merchant.com.adyen.readypay.test) which is registered at the PSP and the RSO app manifest.
                          "\"ApplicationDataBase64\":\"" + application_data_b64 + "\"," +   // Application data that is to be carried and preserved by ReadyPay as it which can be used to validate or match a specific tx.
                          "\"ReadyPayUpdateUrl\":\"" + url_paymentupdate + "\"," +
                          "\"CountryCode\":\"US\"," +
                          "\"CurrencyCode\":\"USD\"," +
                          "\"RequireBillingPostalAddress\":true," +
                          "\"RequireBillingEmailAddress\":true," +
                          "\"RequireBillingPhoneNumber\":true," +
                          "\"RequireShippingPostalAddress\":true," +
                          "\"RequireShippingEmailAddress\":true," +
                          "\"RequireShippingPhoneNumber\":true," +
                          "\"SupportedNetworks\" : [" +
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
                              "{\"Label\": \"Cart Price\", \"Amount\": " + rp_info_model.OrderTotal.ToString() + ", \"IsFinal\": true}," +
                              "{\"Label\": \"Tax\", \"Amount\": " + order_tax.ToString() + ", \"IsFinal\": true}," +
                              "{\"Label\": \"Total\", \"Amount\":" + (rp_info_model.OrderTotal + order_tax).ToString() + ", \"IsFinal\": true}" +
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
