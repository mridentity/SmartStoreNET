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

        //[HttpPost("UpdateShippingCostForContact")]         // The API call path is: ~/api/UpdateShippingCostForContact
        //public async Task<IActionResult> UpdateShippingCostForContact([FromBody]JObject json_obj)
        //{
        //    string json_str = json_obj.ToString();
        //    return Ok(json_obj);
        //}

        //[HttpPost("ShippingMethodsForContact")]         // The API call path is: ~/api/ShippingMethodsForContact
        //public async Task<IActionResult> ShippingMethodsForContact([FromBody]JObject json_obj)
        //{
        //    string json_str = json_obj.ToString();
        //    return Ok(json_obj);
        //}

        //[HttpPost("UpdateShippingCostForShippingMethod")]         // The API call path is: ~/api/UpdateShippingCostForShippingMethod
        //public async Task<IActionResult> UpdateShippingCostForShippingMethod([FromBody]JObject json_obj)
        //{
        //    string json_str = json_obj.ToString();
        //    return Ok(json_obj);
        //}

    }
}