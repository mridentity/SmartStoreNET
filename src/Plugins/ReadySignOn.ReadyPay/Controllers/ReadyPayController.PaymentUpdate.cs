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
using SmartStore.Core.Domain.Customers;
using SmartStore.Services.Customers;
using SmartStore.Core.Domain.Orders;

namespace ReadySignOn.ReadyPay.Controllers
{
    public partial class ReadyPayController
    {
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> UpdatePaymentMethodCost()
        {
            StreamReader stream = new StreamReader(Request.InputStream);
            string json_request = stream.ReadToEnd();

            JObject jInput = JObject.Parse(json_request);

            try
            {
                //Get the product IDs of this payment
                string product_id_str = jInput["appDataB64"].ToString();
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
                string selected_payment = jInput["paymentName"].ToString();

                if(string.IsNullOrEmpty(selected_payment))
                {
                    throw new ArgumentException("//Payment method name cannot be null or empty when updating payment method cost.");
                }

                //Get payment network name
                string payment_network = jInput["paymentNetwork"].ToString();

                if(string.IsNullOrEmpty(payment_network))
                {
                    throw new ArgumentException("//Payment network cannot be null or empty when updating payment method cost.");
                }

                //Get payment type
                string payment_type = jInput["PaymentType"].ToString();

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
            StreamReader stream = new StreamReader(Request.InputStream);
            string json_request = stream.ReadToEnd();

            JObject jInput = JObject.Parse(json_request);

            return Json(jInput, "application/json", Encoding.UTF8);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> ShippingMethodsForContact()
        {
            StreamReader stream = new StreamReader(Request.InputStream);
            string json_request = stream.ReadToEnd();

            JObject jInput = JObject.Parse(json_request);

            try
            {
                //Get the product IDs of this payment
                string product_id_str = jInput["appDataB64"].ToString();
                if (string.IsNullOrEmpty(product_id_str))
                {
                    throw new ArgumentException("//Product ID cannot be null or empty when updating shipping methods for contract.");
                }

                int[] product_ids = Encoding.UTF8.GetString(Convert.FromBase64String(product_id_str)).ToIntArray();
                if (product_ids.IsNullOrEmpty())
                {
                    throw new ArgumentException("//Product ID is required when updating shipping methods for contract.");
                }

                //Get street address
                string street_address = jInput["street"].ToString();

                if (string.IsNullOrEmpty(street_address))
                {
                    throw new ArgumentException("//Street address cannot be null or empty when updating shipping methods for contract.");
                }

                //Get cit
                string city = jInput["city"].ToString();

                if (string.IsNullOrEmpty(city))
                {
                    throw new ArgumentException("//City cannot be null or empty when updating shipping methods for contract.");
                }

                //Get state
                string state = jInput["state"].ToString();

                if (string.IsNullOrEmpty(state))
                {
                    throw new ArgumentException("//State cannot be null or empty when updating shipping methods for contract.");
                }

                //Get postal code
                string postal_code = jInput["postalCode"].ToString();

                if (string.IsNullOrEmpty(postal_code))
                {
                    throw new ArgumentException("//PostalCode cannot be null or empty when updating shipping methods for contract.");
                }

                //Get country
                string country = jInput["country"].ToString();

                if (string.IsNullOrEmpty(country))
                {
                    throw new ArgumentException("//Country cannot be null or empty when updating shipping methods for contract.");
                }

                //Get country code
                string iso_country_code = jInput["isoCountryCode"].ToString();

                if (string.IsNullOrEmpty(iso_country_code))
                {
                    throw new ArgumentException("//Country code cannot be null or empty when updating shipping methods for contract.");
                }

                // Generate list of shipping options
                var cart = new List<OrganizedShoppingCartItem>();
                cart.Clear();
                cart.Add(new OrganizedShoppingCartItem(new ShoppingCartItem { ProductId = 1 }));

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return new EmptyResult();
            }

            return Json(jInput, "application/json", Encoding.UTF8);
        }
    }
}