﻿using SmartStore.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReadySignOn.ReadyConnect
{
    public class ReadyConnectExternalAuthSettings : ISettings
    {
        public ReadyConnectExternalAuthSettings()
        {
            UseSandbox = true;
        }
        public bool UseSandbox { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }
}