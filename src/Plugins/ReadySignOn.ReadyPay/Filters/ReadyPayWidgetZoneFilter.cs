using SmartStore;
using SmartStore.Services;
using SmartStore.Services.Payments;
using SmartStore.Web.Framework.UI;
using SmartStore.Web.Models.ShoppingCart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ReadySignOn.ReadyPay.Filters
{
    public class ReadyPayWidgetZoneFilter : IActionFilter, IResultFilter
    {
        private readonly Lazy<IWidgetProvider> _widgetProvider;
        private readonly Lazy<IPaymentService> _paymentService;
        private readonly Lazy<ICommonServices> _services;
        private readonly Lazy<ReadyPaySettings> _readyPaySettings;

        public ReadyPayWidgetZoneFilter(
			Lazy<IWidgetProvider> widgetProvider,
			Lazy<IPaymentService> paymentService,
			Lazy<ICommonServices> services,
            Lazy<ReadyPaySettings> readyPaySettings)
        {
			_widgetProvider = widgetProvider;
			_paymentService = paymentService;
			_services = services;
            _readyPaySettings = readyPaySettings;
        }
        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
        }

        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
        }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
            if (filterContext.IsChildAction)
                return;

            // should only run on a full view rendering result
            var result = filterContext.Result as ViewResultBase;
            if (result == null)
                return;

            var controller = filterContext.RouteData.Values["controller"] as string;
            var action = filterContext.RouteData.Values["action"] as string;

            if (action.IsCaseInsensitiveEqual("OffCanvasShoppingCart") && controller.IsCaseInsensitiveEqual("ShoppingCart"))
            {
                var model = filterContext.Controller.ViewData.Model as MiniShoppingCartModel;

                if (model != null && model.DisplayCheckoutButton && _readyPaySettings.Value.ShowButtonInMiniShoppingCart)
                {
                    if (_paymentService.Value.IsPaymentMethodActive(ReadySignOn.ReadyPay.Plugin.SystemName, _services.Value.StoreContext.CurrentStore.Id))
                    {
                        _widgetProvider.Value.RegisterAction("offcanvas_cart_summary", "MiniShoppingCart", "ReadyPay", new { area = "ReadySignOn.ReadyPay" });
                    }
                }
            }
        }
    }
}