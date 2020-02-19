using Newtonsoft.Json.Linq;
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

namespace ReadySignOn.ReadyPay.Controllers
{
    public partial class ReadyPayController
    {
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> UpdatePaymentMethodCost()
        {
            StreamReader stream = new StreamReader(Request.InputStream);
            string json_str = stream.ReadToEnd();

            JObject jObj = JObject.Parse(json_str);

            try
            {
                //Get the product IDs of this payment
                string product_id_str = jObj["appDataB64"].ToString();
                if (string.IsNullOrEmpty(product_id_str))
                {
                    throw new ArgumentException("//Product ID cannot be null or empty when updating payment method cost.");
                }

                int[] product_ids = Encoding.UTF8.GetString(Convert.FromBase64String(product_id_str)).ToIntArray();
                if (product_ids.IsNullOrEmpty())
                {
                    throw new ArgumentException("//Product ID is required when updating payment method cost.");
                }

                //Get selected payment name
                string selected_payment = jObj["paymentName"].ToString();

                if(string.IsNullOrEmpty(selected_payment))
                {
                    throw new ArgumentException("//Payment method name cannot be null or empty when updating payment method cost.");
                }

                //Get payment network name
                string payment_network = jObj["paymentNetwork"].ToString();

                if(string.IsNullOrEmpty(payment_network))
                {
                    throw new ArgumentException("//Payment network cannot be null or empty when updating payment method cost.");
                }

                //Get payment type
                string payment_type = jObj["PaymentType"].ToString();

                if(string.IsNullOrEmpty(payment_type))
                {
                    throw new ArgumentException("//Payment type cannot be null or empty when updating payment method cost.");
                }



            }
            catch(Exception ex)
            {
                Logger.Error(ex);
                return new EmptyResult();
            }

            return Json(jObj, "application/json", Encoding.UTF8);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> UpdateShippingCostForContact()
        {
            StreamReader stream = new StreamReader(Request.InputStream);
            string json_str = stream.ReadToEnd();

            JObject jObj = JObject.Parse(json_str);

            return Json(jObj, "application/json", Encoding.UTF8);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> ShippingMethodsForContact()
        {
            StreamReader stream = new StreamReader(Request.InputStream);
            string json_str = stream.ReadToEnd();

            JObject jObj = JObject.Parse(json_str);

            return Json(jObj, "application/json", Encoding.UTF8);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> UpdateShippingCostForShippingMethod()
        {
            StreamReader stream = new StreamReader(Request.InputStream);
            string json_str = stream.ReadToEnd();

            JObject jObj = JObject.Parse(json_str);

            return Json(jObj, "application/json", Encoding.UTF8);
        }
    }
}