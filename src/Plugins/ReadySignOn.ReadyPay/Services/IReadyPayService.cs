using ReadySignOn.ReadyPay.Models;
using SmartStore.Core.Domain.Customers;
using SmartStore.Core.Domain.Orders;
using SmartStore.Web.Models.Checkout;
using System.Collections.Generic;

namespace ReadySignOn.ReadyPay.Services
{
    public interface IReadyPayService 
    {
        ReadyPayment ProcessReadyPay(ReadyPayPaymentInfoModel readypay_request);
        CheckoutShippingMethodModel GetShippingMethodModel(Customer customer, List<OrganizedShoppingCartItem> cart);
    }
}
