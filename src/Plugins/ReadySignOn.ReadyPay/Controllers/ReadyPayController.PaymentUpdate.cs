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

            //http://techxposer.com/2017/05/03/do-we-need-to-use-newtonsoft-json-jsonconvert-deserializeobject-or-newtonsoft-json-linq-jtoken-parse/
            dynamic jResult = Newtonsoft.Json.JsonConvert.DeserializeObject(json_str);

            //https://stackoverflow.com/questions/42360139/asp-net-core-return-json-with-status-code
            var json_result = new JsonResult();
            json_result.Data = jResult;
            json_result.ContentType = "application/json";
            json_result.ContentEncoding = Encoding.UTF8;
            return json_result;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> UpdateShippingCostForContact()
        {
            StreamReader stream = new StreamReader(Request.InputStream);
            string json_str = stream.ReadToEnd();

            //http://techxposer.com/2017/05/03/do-we-need-to-use-newtonsoft-json-jsonconvert-deserializeobject-or-newtonsoft-json-linq-jtoken-parse/
            dynamic jResult = Newtonsoft.Json.JsonConvert.DeserializeObject(json_str);

            //https://stackoverflow.com/questions/42360139/asp-net-core-return-json-with-status-code
            var json_result = new JsonResult();
            json_result.Data = jResult;
            json_result.ContentType = "application/json";
            json_result.ContentEncoding = Encoding.UTF8;
            return json_result;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> ShippingMethodsForContact()
        {
            StreamReader stream = new StreamReader(Request.InputStream);
            string json_str = stream.ReadToEnd();

            //http://techxposer.com/2017/05/03/do-we-need-to-use-newtonsoft-json-jsonconvert-deserializeobject-or-newtonsoft-json-linq-jtoken-parse/
            dynamic jResult = Newtonsoft.Json.JsonConvert.DeserializeObject(json_str);

            //https://stackoverflow.com/questions/42360139/asp-net-core-return-json-with-status-code
            var json_result = new JsonResult();
            json_result.Data = jResult;
            json_result.ContentType = "application/json";
            json_result.ContentEncoding = Encoding.UTF8;
            return json_result;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> UpdateShippingCostForShippingMethod()
        {
            StreamReader stream = new StreamReader(Request.InputStream);
            string json_str = stream.ReadToEnd();

            //http://techxposer.com/2017/05/03/do-we-need-to-use-newtonsoft-json-jsonconvert-deserializeobject-or-newtonsoft-json-linq-jtoken-parse/
            dynamic jResult = Newtonsoft.Json.JsonConvert.DeserializeObject(json_str);

            //https://stackoverflow.com/questions/42360139/asp-net-core-return-json-with-status-code
            var json_result = new JsonResult();
            json_result.Data = jResult;
            json_result.ContentType = "application/json";
            json_result.ContentEncoding = Encoding.UTF8;
            return json_result;
        }

    }
}