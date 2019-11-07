using ReadySignOn.ReadyPay.Models;
using SmartStore.Services.Authentication.External;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadySignOn.ReadyPay.Services
{
    public interface IReadyPayService : IExternalProviderAuthorizer
    {
		void SetupConfiguration(ReadyPayConfigurationModel model);
    }
}
