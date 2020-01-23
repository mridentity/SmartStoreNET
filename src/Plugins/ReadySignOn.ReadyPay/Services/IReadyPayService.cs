using ReadySignOn.ReadyPay.Models;

namespace ReadySignOn.ReadyPay.Services
{
    public interface IReadyPayService 
    {
        ReadyPayment ProcessReadyPay(ReadyPayPaymentInfoModel readypay_request);
    }
}
