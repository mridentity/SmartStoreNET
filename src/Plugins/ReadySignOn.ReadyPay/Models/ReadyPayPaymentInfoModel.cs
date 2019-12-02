﻿using SmartStore.Web.Framework.Modelling;
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
        public bool CurrentPageIsBasket { get; set; }
        public string SubmitButtonImageUrl { get; set; }
        public string LoaderImageUrl { get; set; }
        public decimal? OrderTotal { get; set; }
        public string ProductId { get; set; }
    }
}