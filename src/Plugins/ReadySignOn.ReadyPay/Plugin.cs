using System;
using System.Collections.Generic;
using System.Web.Routing;
using SmartStore;
using SmartStore.Core.Logging;
using SmartStore.Core.Plugins;
using SmartStore.Services;
using SmartStore.Services.Cms;
using SmartStore.Services.Directory;
using SmartStore.Services.Payments;
using SmartStore.Web.Models.Catalog;
using SmartStore.Web.Models.Order;
using SmartStore.Web.Models.ShoppingCart;

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
            throw new NotImplementedException();
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
                "productdetails_add_info",
                "offcavas_cart_summary",
                "order_summary_totals_after",
                "orderdetails_page_aftertotal",
                "invoice_aftertotal"
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
                actionName = "InPlaceReadyPay";
                controllerName = "ReadyPay";
            }

            if (widgetZone == "productdetails_add_info" && settings.ShowInPlaceReadyPay_productdetails_add_info)
            {
                actionName = "InPlaceReadyPay";
                controllerName = "ReadyPay";
            }

            if (widgetZone == "offcavas_cart_summary" && settings.ShowInPlaceReadyPay_offcavas_cart_summary)
            {
                actionName = "InPlaceReadyPay";
                controllerName = "ReadyPay";
            }

            if (widgetZone == "order_summary_totals_after" && settings.ShowInPlaceReadyPay_order_summary_totals_after)
            {
                actionName = "InPlaceReadyPay";
                controllerName = "ReadyPay";
            }

            if (widgetZone == "orderdetails_page_aftertotal" && settings.ShowInPlaceReadyPay_orderdetails_page_aftertotal)
            {
                actionName = "InPlaceReadyPay";
                controllerName = "ReadyPay";
            }

            if (widgetZone == "invoice_aftertotal" && settings.ShowInPlaceReadyPay_invoice_aftertotal)
            {
                actionName = "InPlaceReadyPay";
                controllerName = "ReadyPay";
            }

            return;

            if (widgetZone == "productdetails_add_info")
            {
                var viewModel = model as ProductDetailsModel;
                if (viewModel != null)
                {
                    var price = viewModel.ProductPrice.PriceWithDiscountValue > decimal.Zero
                        ? viewModel.ProductPrice.PriceWithDiscountValue
                        : viewModel.ProductPrice.PriceValue;

                    if (price > decimal.Zero)
                    {
                        actionName = "Promotion";
                        controllerName = "PayPalInstalments";

                        // Convert price because it is in working currency.
                        price = _currencyService.Value.ConvertToPrimaryStoreCurrency(price, _services.WorkContext.WorkingCurrency);

                        routeValues.Add("origin", "productpage");
                        routeValues.Add("amount", price);
                    }
                }
            }
            else if (widgetZone == "order_summary_totals_after")
            {
                var viewModel = model as ShoppingCartModel;
                if (viewModel != null && viewModel.IsEditable)
                {
                    actionName = "Promotion";
                    controllerName = "PayPalInstalments";

                    routeValues.Add("origin", "cart");
                    routeValues.Add("amount", decimal.Zero);
                }
            }
            else if (widgetZone == "orderdetails_page_aftertotal" || widgetZone == "invoice_aftertotal")
            {
                var viewModel = model as OrderDetailsModel;
                if (viewModel != null)
                {
                    actionName = "OrderDetails";
                    controllerName = "PayPalInstalments";

                    routeValues.Add("orderId", viewModel.Id);
                    routeValues.Add("print", widgetZone.IsCaseInsensitiveEqual("invoice_aftertotal"));
                }
            }
        }

        #endregion
    }
}
