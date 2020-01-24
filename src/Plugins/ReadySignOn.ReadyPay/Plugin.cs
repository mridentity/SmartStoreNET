using ReadySignOn.ReadyPay.Controllers;
using ReadySignOn.ReadyPay.Models;
using SmartStore.Core.Logging;
using SmartStore.Core.Plugins;
using SmartStore.Services;
using SmartStore.Services.Cms;
using SmartStore.Services.Directory;
using SmartStore.Services.Payments;
using SmartStore.Web.Models.Catalog;
using System;
using System.Collections.Generic;
using System.Web.Routing;

namespace ReadySignOn.ReadyPay
{
    [SystemName("ReadySignOn.ReadyPay")]
    [FriendlyName("ReadyPay")]
    public class Plugin : PaymentPluginBase, IWidget, IConfigurable
    {
        private readonly ICommonServices _services;
        private readonly Lazy<ICurrencyService> _currencyService;

        public Plugin(
            ICommonServices services,
            Lazy<ICurrencyService> currencyService)
		{
            _services = services;
            _currencyService = currencyService;

			Logger = NullLogger.Instance;
		}

		public ILogger Logger { get; set; }

		public static string SystemName => "ReadySignOn.ReadyPay";

		public override void Install()
		{
            _services.Settings.SaveSetting(new ReadyPaySettings());

            _services.Localization.ImportPluginResourcesFromXml(this.PluginDescriptor);

			base.Install();
		}

		public override void Uninstall()
		{
            //DeleteWebhook(_services.Settings.LoadSetting<PayPalPlusPaymentSettings>(), PayPalPlusProvider.SystemName);
            //DeleteWebhook(_services.Settings.LoadSetting<PayPalInstalmentsSettings>(), PayPalInstalmentsProvider.SystemName);

            _services.Settings.DeleteSetting<ReadyPaySettings>();

            _services.Localization.DeleteLocaleStringResources(PluginDescriptor.ResourceRootKey);

			base.Uninstall();
		}

        public override ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            throw new NotImplementedException();
        }

        public override void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "ReadyPay";
            routeValues = new RouteValueDictionary { { "Namespaces", "ReadySignOn.ReadyPay.Controllers" }, { "area", SystemName } };
        }

        public override Type GetControllerType()
        {
            return typeof(ReadyPayController);
        }

        public override PaymentMethodType PaymentMethodType
		{
			get { return PaymentMethodType.StandardAndButton; }
		}

        /// <summary>
        /// Gets a route for payment info
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public override void GetPaymentInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "PaymentInfo";
            controllerName = "ReadyPay";
            routeValues = new RouteValueDictionary() { { "area", "ReadySignOn.ReadyPay" } };
        }

        public override bool SupportCapture
        {
            get { return true; }
        }

        public override bool SupportPartiallyRefund
        {
            get { return true; }
        }

        public override bool SupportRefund
        {
            get { return true; }
        }

        public override bool SupportVoid
        {
            get { return true; }
        }

        #region Widget related

        public IList<string> GetWidgetZones()
        {
            return new List<string>
            {
                "productbox_add_info",
                "productdetails_add_info"
            };
        }

        public void GetDisplayWidgetRoute(string widgetZone, object model, int storeId, out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            var settings = _services.Settings.LoadSetting<ReadyPaySettings>(_services.StoreContext.CurrentStore.Id);

            actionName = controllerName = null;
            routeValues = new RouteValueDictionary
            {
                { "Namespaces", "ReadySignOn.ReadyPay.Controllers" },
                { "area", SystemName }
            };

            if(widgetZone == "productbox_add_info" && settings.ShowInPlaceReadyPay_productbox_add_info)
            {

                var summaryItem = model as ProductSummaryModel.SummaryItem;
                var priceModel = summaryItem.Price;

                var product_id = summaryItem.Id;
                var product_sku = summaryItem.Sku;
                var product_name = summaryItem.Name;
                var product_price = priceModel.PriceValue;

                SetupInPlaceReadyPayRouteValues(ref actionName, ref controllerName, routeValues, product_id, product_sku, product_name, ref product_price);
            }

            if (widgetZone == "productdetails_add_info" && settings.ShowInPlaceReadyPay_productdetails_add_info)
            {
                var product_detail = model as ProductDetailsModel;

                var product_id = product_detail.Id;
                var product_sku = product_detail.Sku;
                var product_name = product_detail.Name;
                var product_price = product_detail.ProductPrice.PriceValue;

                SetupInPlaceReadyPayRouteValues(ref actionName, ref controllerName, routeValues, product_id, product_sku, product_name, ref product_price);
            }

            return;
        }

        private void SetupInPlaceReadyPayRouteValues(ref string actionName, ref string controllerName, RouteValueDictionary routeValues, 
                                                        int product_id, 
                                                        string product_sku, 
                                                        SmartStore.Services.Localization.LocalizedValue<string> product_name, 
                                                        ref decimal product_price)
        {
            if (product_price > decimal.Zero)
            {
                actionName = "InPlaceReadyPay";
                controllerName = "ReadyPay";

                // Convert price because it is in working currency.
                product_price = _currencyService.Value.ConvertToPrimaryStoreCurrency(product_price, _services.WorkContext.WorkingCurrency);

                routeValues.Add("product_id", product_id);
                routeValues.Add("product_sku", product_sku);
                routeValues.Add("product_name", product_name);
                routeValues.Add("product_price", product_price);
            }
        }
        #endregion
    }
}
