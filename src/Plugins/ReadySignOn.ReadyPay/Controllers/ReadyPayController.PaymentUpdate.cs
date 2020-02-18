using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace ReadySignOn.ReadyPay.Controllers
{
    public partial class ReadyPayController
    {
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> UpdatePaymentMethodCost(JObject json_obj)
        {
            //https://forums.asp.net/t/2126864.aspx?How+to+receive+JSON+object+in+POST+using+web+api
            string json_str = json_obj.ToString();
            //https://stackoverflow.com/questions/42360139/asp-net-core-return-json-with-status-code
            var json_result = new JsonResult();
            json_result.Data = json_obj;
            json_result.ContentType = "application/json";
            json_result.ContentEncoding = Encoding.UTF8;
            return json_result;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> UpdateShippingCostForContact(JObject json_obj)
        {
            string json_str = json_obj.ToString();
            var json_result = new JsonResult();
            json_result.Data = json_obj;
            json_result.ContentType = "application/json";
            json_result.ContentEncoding = Encoding.UTF8;
            return json_result;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> ShippingMethodsForContact(JObject json_obj)
        {
            string json_str = json_obj.ToString();
            var json_result = new JsonResult();
            json_result.Data = json_obj;
            json_result.ContentType = "application/json";
            json_result.ContentEncoding = Encoding.UTF8;
            return json_result;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> UpdateShippingCostForShippingMethod(JObject json_obj)
        {
            string json_str = json_obj.ToString();
            var json_result = new JsonResult();
            json_result.Data = json_obj;
            json_result.ContentType = "application/json";
            json_result.ContentEncoding = Encoding.UTF8;
            return json_result;
        }

    }
}