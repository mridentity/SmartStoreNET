using SmartStore.Core.Domain.Common;
using SmartStore.Core.Domain.Shipping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReadySignOn.ReadyPay.Models
{
    /// <summary>
    /// Represents a payment info holder
    /// </summary>
    [Serializable]
    public partial class ReadyOrderRequest
    {
        public ReadyOrderRequest()
        {
            CustomProperties = new Dictionary<string, CustomPaymentRequestValue>();
            IsMultiOrder = false;
            ShoppingCartItemIds = new List<int>();
        }

        #region ReadyPay related
        public bool IsInPlaceReadyPayOrder { get; set; }
        public decimal OrderSubtotalInclTax { get; set; }
        public decimal OrderSubtotalExclTax { get; set; }
        public decimal OrderShippingInclTax { get; set; }
        public decimal OrderShippingExclTax { get; set; }
        public decimal OrderShippingTaxRate { get; set; }
        public decimal PaymentMethodAdditionalFeeInclTax { get; set; }
        public decimal PaymentMethodAdditionalFeeExclTax { get; set; }
        public decimal PaymentMethodAdditionalFeeTaxRate { get; set; }
        public decimal TaxRate { get; set; }
        public decimal OrderTotalRounding { get; set; }
        public decimal OrderDiscount { get; set; }
        public decimal CreditBalance { get; set; }
        public Address BillingAddress { get; set; }
        public Address ShippingAddress { get; set; }
        public string ShippingMethod { get; set; }
        public string ShippingRateComputationMethodSystemName { get; set; }
        public string VatNumber { get; set; }
        public string CustomerOrderComment { get; set; }
        #endregion
        /// <summary>
        /// Gets or sets a store identifier
        /// </summary>
        public int StoreId { get; set; }

        /// <summary>
        /// Gets or sets a customer
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// Gets or sets an order unique identifier. Used when order is not saved yet (payment gateways that do not redirect a customer to a third-party URL)
        /// </summary>
        public Guid OrderGuid { get; set; }

        /// <summary>
        /// Gets or sets an order total
        /// </summary>
        public decimal OrderTotal { get; set; }

        /// <summary>
        /// Gets or sets an order tax total
        /// </summary>
        public decimal OrderTax { get; set; }

        /// <summary>
        /// Gets or sets a payment method identifier
        /// </summary>
        public string PaymentMethodSystemName { get; set; }

        /// <summary>
        /// Gets or sets a payment method identifier
        /// </summary>
        public bool IsMultiOrder { get; set; }

        /// <summary>
        /// Use that dictionary for any payment method or checkout flow specific data
        /// </summary>
        public Dictionary<string, CustomPaymentRequestValue> CustomProperties { get; set; }

        public IList<int> ShoppingCartItemIds { get; set; }

        #region Payment method specific properties 

        /// <summary>
        /// Gets or sets a credit card type (Visa, Master Card, etc...)
        /// </summary>
        public string CreditCardType { get; set; }

        /// <summary>
        /// Gets or sets a credit card owner name
        /// </summary>
        public string CreditCardName { get; set; }

        /// <summary>
        /// Gets or sets a credit card number
        /// </summary>
        public string CreditCardNumber { get; set; }

        /// <summary>
        /// Gets or sets a credit card expire year
        /// </summary>
        public int CreditCardExpireYear { get; set; }

        /// <summary>
        /// Gets or sets a credit card expire month
        /// </summary>
        public int CreditCardExpireMonth { get; set; }

        /// <summary>
        /// Gets or sets a credit card CVV2 (Card Verification Value)
        /// </summary>
        public string CreditCardCvv2 { get; set; }

        /// <summary>
        /// Gets or sets a paypal payer token (required for Paypal payment methods)
        /// </summary>
        public string PaypalToken { get; set; }

        /// <summary>
        /// Gets or sets a paypal payer identifier (required for Paypal payment methods)
        /// </summary>
        public string PaypalPayerId { get; set; }

        /// <summary>
        /// Gets or sets a google order number (required for Google Checkout)
        /// </summary>
        public string GoogleOrderNumber { get; set; }

        /// <summary>
        /// Gets or sets a purchase order number (required for Purchase Order payment method)
        /// </summary>
        public string PurchaseOrderNumber { get; set; }

        public int CreditCardStartYear { get; set; }
        public int CreditCardStartMonth { get; set; }
        public string CreditCardIssueNumber { get; set; }
        public string DirectDebitAccountHolder { get; set; }
        public string DirectDebitAccountNumber { get; set; }
        public string DirectDebitBankCode { get; set; }
        public string DirectDebitCountry { get; set; }
        public string DirectDebitBankName { get; set; }
        public string DirectDebitIban { get; set; }
        public string DirectDebitBic { get; set; }

        public bool IsShippingMethodSet { get; set; }

        #endregion
    }


    [Serializable]
    public partial class CustomPaymentRequestValue
    {
        /// <summary>
        /// The value of the custom property
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Indicates whether to automatically create a generic attribute if an order has been placed
        /// </summary>
        public bool AutoCreateGenericAttribute { get; set; }
    }
}