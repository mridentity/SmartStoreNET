using ReadySignOn.ReadyPay.Models;

namespace ReadySignOn.ReadyPay.Services
{
    public interface IReadyPayService 
    {
		void SetupConfiguration(ReadyPayConfigurationModel model, int storeScope);
        ReadyPayment ProcessReadyPay(ReadyPayPaymentInfoModel readypay_request);
    }
}
