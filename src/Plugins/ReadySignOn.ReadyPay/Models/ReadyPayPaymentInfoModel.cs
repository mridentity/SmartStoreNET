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
    }
}