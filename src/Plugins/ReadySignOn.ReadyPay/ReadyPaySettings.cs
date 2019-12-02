using SmartStore.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadySignOn.ReadyPay
{
    public class ReadyPaySettings : ISettings
    {
        public ReadyPaySettings()
        {
            IpnChangesPaymentStatus = true;
            AddOrderNotes = true;
            TransactMode = TransactMode.Authorize;
            ShowButtonInMiniShoppingCart = true;

            UseSandbox = true;

            ShowInPlaceReadyPay_invoice_aftertotal 
                = ShowInPlaceReadyPay_offcanvas_cart_summary 
                = ShowInPlaceReadyPay_orderdetails_page_aftertotal 
                = ShowInPlaceReadyPay_order_summary_totals_after 
                = ShowInPlaceReadyPay_productbox_add_info 
                = ShowInPlaceReadyPay_productdetails_add_info 
                = true;
        }

        /// <summary>
        /// Gets or sets a value indicating whether an IPN should change the payment status
        /// </summary>
        public bool IpnChangesPaymentStatus { get; set; }

        public TransactMode TransactMode { get; set; }

        public string PrimaryStoreCurrencyCode { get; set; }

        public bool UseSandbox { get; set; }

        public string MerchantId { get; set; }

        public string ClientSecret { get; set; }

        public string SecretKey { get; set; }

        public string ClientId { get; set; }

        public bool ShowButtonInMiniShoppingCart { get; set; }

        public bool ShowInPlaceReadyPay_productbox_add_info { get; set; }

        public bool ShowInPlaceReadyPay_productdetails_add_info { get; set; }

        public bool ShowInPlaceReadyPay_offcanvas_cart_summary { get; set; }

        public bool ShowInPlaceReadyPay_order_summary_totals_after { get; set; }

        public bool ShowInPlaceReadyPay_orderdetails_page_aftertotal { get; set; }

        public bool ShowInPlaceReadyPay_invoice_aftertotal { get; set; }

        public bool ConfirmedShipment { get; set; }

        public bool NoShipmentAddress { get; set; }

        public int CallbackTimeout { get; set; }

        public decimal DefaultShippingPrice { get; set; }

        public decimal AdditionalFee { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to "additional fee" is specified as percentage. true - percentage, false - fixed value.
        /// </summary>
        public bool AdditionalFeePercentage { get; set; }

        public bool AddOrderNotes { get; set; }

        public bool InformCustomerAboutErrors { get; set; }

        public bool InformCustomerAddErrors { get; set; }
    }

    /// <summary>
    /// Represents payment processor transaction mode
    /// </summary>
    public enum TransactMode
    {
        Authorize = 1,
        AuthorizeAndCapture = 2
    }
}
