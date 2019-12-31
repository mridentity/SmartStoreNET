using System.Collections.Generic;
using ReadySignOn.ReadyPay.Models;
using SmartStore.Services.Orders;

namespace ReadySignOn.ReadyPay.Services
{
    public interface IReadyPayOrders
    {
        PlaceOrderResult PlaceOrder(ReadyOrderRequest processPaymentRequest, Dictionary<string, string> extraData);
    }
}