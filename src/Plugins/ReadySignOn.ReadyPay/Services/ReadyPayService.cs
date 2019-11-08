using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReadySignOn.ReadyPay.Models;
using SmartStore.Services;
using SmartStore.Services.Authentication.External;

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
        public AuthorizeState Authorize(string returnUrl, bool? verifyResponse = null)
        {
            throw new NotImplementedException();
        }

        public void SetupConfiguration(ReadyPayConfigurationModel model, int storeScope)
        {
            var store = storeScope == 0
                ? _services.StoreContext.CurrentStore
                : _services.StoreService.GetStoreById(storeScope);

            model.PrimaryStoreCurrencyCode = store.PrimaryStoreCurrency.CurrencyCode;

        }
    }
}
