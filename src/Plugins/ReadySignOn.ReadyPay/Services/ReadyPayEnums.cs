using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadySignOn.ReadyPay.Services
{
	public enum ReadyPayResultType
	{
		None = 0,
		PluginView,
		Redirect,
		Unauthorized
	}
}
