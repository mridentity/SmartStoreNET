using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReadySignOn.ReadyPay.Models;
using SmartStore.Services.Authentication.External;

namespace ReadySignOn.ReadyPay.Services
{
    public class ReadyPayService : IReadyPayService
    {
        public AuthorizeState Authorize(string returnUrl, bool? verifyResponse = null)
        {
            throw new NotImplementedException();
        }

        public void SetupConfiguration(ReadyPayConfigurationModel model)
        {
            throw new NotImplementedException();
        }
    }
}
