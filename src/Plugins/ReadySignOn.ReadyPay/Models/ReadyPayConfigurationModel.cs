using SmartStore.Web.Framework;
using SmartStore.Web.Framework.Modelling;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace ReadySignOn.ReadyPay.Models
{
    public class ReadyPayConfigurationModel : ModelBase
    {
        public string PrimaryStoreCurrencyCode { get; set; }

        [SmartResourceDisplayName("Plugins.Payments.ReadyPay.UseSandbox")]
        public bool UseSandbox { get; set; }

        [SmartResourceDisplayName("Plugins.Payments.ReadyPay.MerchantId")]
        public string MerchantId { get; set; }

        [SmartResourceDisplayName("Plugins.Payments.ReadyPay.ClientSecret")]
        [DataType(DataType.Password)]
        public string ClientSecret { get; set; }

        [SmartResourceDisplayName("Plugins.Payments.ReadyPay.ClientId")]
        public string ClientId { get; set; }

        [SmartResourceDisplayName("Plugins.Payments.ReadyPay.ShowButtonInMiniShoppingCart")]
        public bool ShowButtonInMiniShoppingCart { get; set; }

        [SmartResourceDisplayName("Plugins.Payments.ReadyPay.ShowInPlaceReadyPay_productbox_add_info")]
        public bool ShowInPlaceReadyPay_productbox_add_info { get; set; }

        [SmartResourceDisplayName("Plugins.Payments.ReadyPay.ShowInPlaceReadyPay_productdetails_add_info")]
        public bool ShowInPlaceReadyPay_productdetails_add_info { get; set; }

        [SmartResourceDisplayName("Plugins.Payments.ReadyPay.ShowInPlaceReadyPay_offcanvas_cart_summary")]
        public bool ShowInPlaceReadyPay_offcanvas_cart_summary { get; set; }

        [SmartResourceDisplayName("Plugins.Payments.ReadyPay.ShowInPlaceReadyPay_order_summary_totals_after")]
        public bool ShowInPlaceReadyPay_order_summary_totals_after { get; set; }

        [SmartResourceDisplayName("Plugins.Payments.ReadyPay.ShowInPlaceReadyPay_orderdetails_page_aftertotal")]
        public bool ShowInPlaceReadyPay_orderdetails_page_aftertotal { get; set; }

        [SmartResourceDisplayName("Plugins.Payments.ReadyPay.ShowInPlaceReadyPay_invoice_aftertotal")]
        public bool ShowInPlaceReadyPay_invoice_aftertotal { get; set; }

        [SmartResourceDisplayName("Plugins.Payments.ReadyPay.ConfirmedShipment")]
        public bool ConfirmedShipment { get; set; }

        [SmartResourceDisplayName("Plugins.Payments.ReadyPay.NoShipmentAddress")]
        public bool NoShipmentAddress { get; set; }

        [SmartResourceDisplayName("Plugins.Payments.ReadyPay.CallbackTimeout")]
        public int CallbackTimeout { get; set; }

        [SmartResourceDisplayName("Plugins.Payments.ReadyPay.DefaultShippingPrice")]
        public decimal DefaultShippingPrice { get; set; }

        [SmartResourceDisplayName("Admin.Configuration.Payment.Methods.AdditionalFee")]
        public decimal AdditionalFee { get; set; }

        [SmartResourceDisplayName("Admin.Configuration.Payment.Methods.AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }

        [SmartResourceDisplayName("Plugins.Payments.ReadyPay.AddOrderNotes")]
        public bool AddOrderNotes { get; set; }

        [SmartResourceDisplayName("Plugins.Payments.ReadyPay.InformCustomerAboutErrors")]
        public bool InformCustomerAboutErrors { get; set; }

        [SmartResourceDisplayName("Plugins.Payments.ReadyPay.InformCustomerAddErrors")]
        public bool InformCustomerAddErrors { get; set; }

    }
}
