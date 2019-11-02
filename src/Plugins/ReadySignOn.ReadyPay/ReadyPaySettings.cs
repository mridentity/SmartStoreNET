using SmartStore.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadySignOn.ReadyPay
{
    class ReadyPaySettings : ISettings
    {
        public ReadyPaySettings()
        {
            IpnChangesPaymentStatus = true;
            AddOrderNotes = true;
            TransactMode = TransactMode.Authorize;
        }

        public bool UseSandbox { get; set; }

        public bool AddOrderNotes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to "additional fee" is specified as percentage. true - percentage, false - fixed value.
        /// </summary>
        public bool AdditionalFeePercentage { get; set; }

        public decimal AdditionalFee { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether an IPN should change the payment status
        /// </summary>
        public bool IpnChangesPaymentStatus { get; set; }

        public TransactMode TransactMode { get; set; }
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
