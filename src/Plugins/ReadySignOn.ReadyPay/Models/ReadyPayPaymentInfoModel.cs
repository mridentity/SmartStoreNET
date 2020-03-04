using SmartStore.Web.Framework.Modelling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReadySignOn.ReadyPay.Models
{
    public class ReadyPayPaymentInfoModel : ModelBase
    {
        public ReadyPayPaymentInfoModel()
        {

        }

        public string Sentinel { get; set; }
        public string ReadyTicket { get; set; }
        public bool   CurrentPageIsBasket { get; set; }
        public string SubmitButtonImageUrl { get; set; }
        public string LoaderImageUrl { get; set; }
        public decimal CartSubTotal { get; set; }       // All the items of the order excluding shippig and tax
        public decimal TaxTotal { get; set; }           // The total tax amount including shipping tax if any.
        public decimal? ShippingTotal { get; set; }     // The total shipping cost including shipping tax if any
        public decimal ShippingTax { get; set; }        // The tax amount for shipping
        public string AppData { get; set; }             // Base64 encoded application specific data used by readyPay for tracking orders.
    }
}