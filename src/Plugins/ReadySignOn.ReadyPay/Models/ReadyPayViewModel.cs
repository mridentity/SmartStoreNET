using ReadySignOn.ReadyPay.Services;
using SmartStore.Web.Framework.Modelling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadySignOn.ReadyPay.Models
{
    public class ReadyPayViewModel : ModelBase
    {
        public ReadyPayViewModel()
        {
            Result = ReadyPayResultType.PluginView;
        }
        public ReadyPayResultType Result { get; set; }

		public string RedirectAction { get; set; }
		public string RedirectController { get; set; }
    }
}
