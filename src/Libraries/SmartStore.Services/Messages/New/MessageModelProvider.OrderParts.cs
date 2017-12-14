﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web;
using SmartStore.ComponentModel;
using SmartStore.Core.Domain.Common;
using SmartStore.Core.Domain.Media;
using SmartStore.Core.Domain.Orders;
using SmartStore.Core.Domain.Shipping;
using SmartStore.Core.Domain.Tax;
using SmartStore.Core.Html;
using SmartStore.Core.Plugins;
using SmartStore.Services.Catalog;
using SmartStore.Services.Common;
using SmartStore.Services.Customers;
using SmartStore.Services.Directory;
using SmartStore.Services.Localization;
using SmartStore.Services.Media;
using SmartStore.Services.Orders;
using SmartStore.Services.Payments;

namespace SmartStore.Services.Messages
{
	public partial class MessageModelProvider
	{
		protected virtual object CreateModelPart(Order part, MessageContext messageContext)
		{
			Guard.NotNull(messageContext, nameof(messageContext));
			Guard.NotNull(part, nameof(part));

			var allow = new HashSet<string>
			{
				nameof(part.Id),
				nameof(part.OrderNumber),
				nameof(part.OrderGuid),
				nameof(part.StoreId),
				nameof(part.OrderStatus),
				nameof(part.PaymentStatus),
				nameof(part.ShippingStatus),
				nameof(part.CustomerTaxDisplayType),
				nameof(part.TaxRatesDictionary),
				nameof(part.VatNumber),
				nameof(part.AffiliateId),
				nameof(part.CustomerIp),
				nameof(part.CardType),
				nameof(part.CardName),
				nameof(part.MaskedCreditCardNumber),
				nameof(part.DirectDebitAccountHolder),
				nameof(part.DirectDebitBankCode), // TODO: (mc) Liquid > Bank data (?)
				nameof(part.PurchaseOrderNumber),
				nameof(part.PaidDateUtc),
				nameof(part.ShippingMethod),
				nameof(part.PaymentMethodSystemName),
				nameof(part.ShippingRateComputationMethodSystemName),
				nameof(part.CreatedOnUtc)
				// TODO: (mc) Liquid > More whitelisting?
			};

			var m = new HybridExpando(part, allow, MemberOptMethod.Allow);
			var d = m as dynamic;

			d.ID = part.Id;
			d.Billing = CreateModelPart(part.BillingAddress, messageContext);
			d.Shipping = part.ShippingAddress == null ? null : CreateModelPart(part.ShippingAddress, messageContext);
			//d.CustomerFullName = part.BillingAddress.GetFullName();
			d.CustomerEmail = part.BillingAddress.Email;
			d.CustomerComment = part.CustomerOrderComment;
			d.Disclaimer = GetTopic("Disclaimer", messageContext);
			d.ConditionsOfUse = GetTopic("ConditionsOfUse", messageContext);
			d.CreatedOn = ToUserDate(part.CreatedOnUtc, messageContext);

			// Payment method
			var paymentMethodName = part.PaymentMethodSystemName;
			var paymentMethod = _services.Resolve<IProviderManager>().GetProvider<IPaymentMethod>(part.PaymentMethodSystemName);
			if (paymentMethod != null)
			{
				paymentMethodName = GetLocalizedValue(messageContext, paymentMethod.Metadata, nameof(paymentMethod.Metadata.FriendlyName), x => x.FriendlyName);
			}
			d.PaymentMethod = paymentMethodName;

			d.OrderURLForCustomer = part.Customer != null && !part.Customer.IsGuest()
				? BuildActionUrl("Details", "Order", new { id = part.Id, area = "" }, messageContext)
				: "";

			// Overrides
			m.Properties["OrderNumber"] = part.GetOrderNumber();
			m.Properties["AcceptThirdPartyEmailHandOver"] = GetBoolResource(part.AcceptThirdPartyEmailHandOver, messageContext);
			m.Properties["OrderNotes"] = part.OrderNotes.Select(x => CreateModelPart(x, messageContext)).ToList();

			// Items, Totals & Co.
			d.Items = part.OrderItems.Where(x => x.Product != null).Select(x => CreateModelPart(x, messageContext)).ToList();
			d.Totals = CreateOrderTotalsPart(part, messageContext);

			// Checkout Attributes
			if (part.CheckoutAttributeDescription.HasValue())
			{
				d.CheckoutAttributes = HtmlUtils.ConvertPlainTextToTable(HtmlUtils.ConvertHtmlToPlainText(part.CheckoutAttributeDescription));
			}

			PublishModelPartCreatedEvent<Order>(part, m);

			return m;
		}

		protected virtual object CreateOrderTotalsPart(Order order, MessageContext messageContext)
		{
			Guard.NotNull(messageContext, nameof(messageContext));
			Guard.NotNull(order, nameof(order));

			var language = messageContext.Language;
			var currencyService = _services.Resolve<ICurrencyService>();
			var paymentService = _services.Resolve<IPaymentService>();
			var priceFormatter = _services.Resolve<IPriceFormatter>();
			var taxSettings = _services.Settings.LoadSetting<TaxSettings>(messageContext.Store.Id);

			var taxRates = new SortedDictionary<decimal, decimal>();
			string cusTaxTotal = string.Empty;
			string cusDiscount = string.Empty;
			string cusTotal = string.Empty;

			var subTotals = GetSubTotals(order, messageContext);

			// Shipping
			bool dislayShipping = order.ShippingStatus != ShippingStatus.ShippingNotRequired;

			// Payment method fee
			bool displayPaymentMethodFee = true;
			if (order.PaymentMethodAdditionalFeeExclTax == decimal.Zero)
			{
				displayPaymentMethodFee = false;
			}

			// Tax
			bool displayTax = true;
			bool displayTaxRates = true;
			if (taxSettings.HideTaxInOrderSummary && order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax)
			{
				displayTax = false;
				displayTaxRates = false;
			}
			else
			{
				if (order.OrderTax == 0 && taxSettings.HideZeroTax)
				{
					displayTax = false;
					displayTaxRates = false;
				}
				else
				{
					taxRates = new SortedDictionary<decimal, decimal>();
					foreach (var tr in order.TaxRatesDictionary)
					{
						taxRates.Add(tr.Key, currencyService.ConvertCurrency(tr.Value, order.CurrencyRate));
					}

					displayTaxRates = taxSettings.DisplayTaxRates && taxRates.Count > 0;
					displayTax = !displayTaxRates;

					var orderTaxInCustomerCurrency = currencyService.ConvertCurrency(order.OrderTax, order.CurrencyRate);
					string taxStr = priceFormatter.FormatPrice(orderTaxInCustomerCurrency, true, order.CustomerCurrencyCode, false, language);
					cusTaxTotal = taxStr;
				}
			}

			// Discount
			bool dislayDiscount = false;
			if (order.OrderDiscount > decimal.Zero)
			{
				var orderDiscountInCustomerCurrency = currencyService.ConvertCurrency(order.OrderDiscount, order.CurrencyRate);
				cusDiscount = priceFormatter.FormatPrice(-orderDiscountInCustomerCurrency, true, order.CustomerCurrencyCode, false, language);
				dislayDiscount = true;
			}

			// Total
			var roundingAmount = decimal.Zero;
			var orderTotal = order.GetOrderTotalInCustomerCurrency(currencyService, paymentService, out roundingAmount);
			cusTotal = priceFormatter.FormatPrice(orderTotal, true, order.CustomerCurrencyCode, false, language);

			// Model
			dynamic m = new ExpandoObject();

			m.SubTotal = subTotals.SubTotal;
			m.SubTotalDiscount = subTotals.DisplaySubTotalDiscount ? subTotals.SubTotalDiscount : "";
			m.Shipping = dislayShipping ? subTotals.ShippingTotal : "";
			m.Payment = displayPaymentMethodFee ? subTotals.PaymentFee : "";
			m.Tax = displayTax ? cusTaxTotal : "";
			m.Discount = dislayDiscount ? cusDiscount : "";
			m.Total = cusTotal;

			// TaxRates
			m.TaxRates = !displayTaxRates ? (object[])null : taxRates.Select(x =>
			{
				return new
				{
					Rate = T("Messages.Order.TaxRateLine", language.Id, priceFormatter.FormatTaxRate(x.Key)),
					Value = FormatPrice(x.Value, order, messageContext)
				};
			}).ToArray();


			// Gift Cards
			m.GiftCardUsage = order.GiftCardUsageHistory.Count == 0 ? (object[])null : order.GiftCardUsageHistory.Select(x =>
			{
				return new
				{
					GiftCard = T("Messages.Order.GiftCardInfo", language.Id, x.GiftCard.GiftCardCouponCode),
					UsedAmount = FormatPrice(-x.UsedValue, order, messageContext),
					RemainingAmount = FormatPrice(x.GiftCard.GetGiftCardRemainingAmount(), order, messageContext)
				};
			}).ToArray();

			// Reward Points
			m.RedeemedRewardPoints = order.RedeemedRewardPointsEntry == null ? null : new
			{
				Title = T("Messages.Order.RewardPoints", language.Id, -order.RedeemedRewardPointsEntry.Points),
				Amount = FormatPrice(-order.RedeemedRewardPointsEntry.UsedAmount, order, messageContext)
			};

			return m;
		}

		private (string SubTotal, string SubTotalDiscount, string ShippingTotal, string PaymentFee, bool DisplaySubTotalDiscount) GetSubTotals(Order order, MessageContext messageContext)
		{
			var language = messageContext.Language;
			var currencyService = _services.Resolve<ICurrencyService>();
			var priceFormatter = _services.Resolve<IPriceFormatter>();

			string cusSubTotal = string.Empty;
			string cusSubTotalDiscount = string.Empty;
			string cusShipTotal = string.Empty;
			string cusPaymentMethodFee = string.Empty;
			bool dislaySubTotalDiscount = false;

			var subTotal = order.CustomerTaxDisplayType == TaxDisplayType.ExcludingTax ? order.OrderSubtotalExclTax : order.OrderSubtotalInclTax;
			var subTotalDiscount = order.CustomerTaxDisplayType == TaxDisplayType.ExcludingTax ? order.OrderSubTotalDiscountExclTax : order.OrderSubTotalDiscountInclTax;
			var shipping = order.CustomerTaxDisplayType == TaxDisplayType.ExcludingTax ? order.OrderShippingExclTax : order.OrderShippingInclTax;
			var payment = order.CustomerTaxDisplayType == TaxDisplayType.ExcludingTax ? order.PaymentMethodAdditionalFeeExclTax : order.PaymentMethodAdditionalFeeInclTax;

			// Subtotal
			cusSubTotal = FormatPrice(subTotal, order, messageContext);

			// Discount (applied to order subtotal)
			var orderSubTotalDiscount = currencyService.ConvertCurrency(subTotalDiscount, order.CurrencyRate);
			if (orderSubTotalDiscount > decimal.Zero)
			{
				cusSubTotalDiscount = priceFormatter.FormatPrice(-orderSubTotalDiscount, true, order.CustomerCurrencyCode, language, false);
				dislaySubTotalDiscount = true;
			}

			// Shipping
			var orderShipping = currencyService.ConvertCurrency(shipping, order.CurrencyRate);
			cusShipTotal = priceFormatter.FormatShippingPrice(orderShipping, true, order.CustomerCurrencyCode, language, false);

			// Payment method additional fee
			var paymentMethodAdditionalFee = currencyService.ConvertCurrency(payment, order.CurrencyRate);
			cusPaymentMethodFee = priceFormatter.FormatPaymentMethodAdditionalFee(paymentMethodAdditionalFee, true, order.CustomerCurrencyCode, language, false);

			return (cusSubTotal, cusSubTotalDiscount, cusShipTotal, cusPaymentMethodFee, dislaySubTotalDiscount);
		}

		protected virtual object CreateModelPart(OrderItem part, MessageContext messageContext)
		{
			Guard.NotNull(messageContext, nameof(messageContext));
			Guard.NotNull(part, nameof(part));

			var productAttributeParser = _services.Resolve<IProductAttributeParser>();
			var downloadService = _services.Resolve<IDownloadService>();
			var order = part.Order;
			var isNet = order.CustomerTaxDisplayType == TaxDisplayType.ExcludingTax;
			var product = part.Product;
			product.MergeWithCombination(part.AttributesXml, productAttributeParser);

			var m = new Dictionary<string, object>
			{
				{ "DownloadUrl", !downloadService.IsDownloadAllowed(part) ? "" : BuildActionUrl("GetDownload", "Download", new { id = part.OrderItemGuid, area = "" }, messageContext) },
				{ "AttributeDescription", part.AttributeDescription },
				{ "Weight", part.ItemWeight },
				{ "TaxRate", part.TaxRate },
				{ "Qty", part.Quantity },
				{ "UnitPrice", FormatPrice(isNet ? part.UnitPriceExclTax : part.UnitPriceInclTax, part.Order, messageContext) },
				{ "LineTotal", FormatPrice(isNet ? part.PriceExclTax : part.PriceInclTax, part.Order, messageContext) },
				{ "Product", CreateModelPart(product, messageContext, part.AttributesXml) }
			};
			
			PublishModelPartCreatedEvent<OrderItem>(part, m);

			return m;
		}

		protected virtual object CreateModelPart(OrderNote part, MessageContext messageContext)
		{
			Guard.NotNull(messageContext, nameof(messageContext));
			Guard.NotNull(part, nameof(part));

			var m = new Dictionary<string, object>
			{
				{ "Id", part.Id },
				{ "CreatedOn", ToUserDate(part.CreatedOnUtc, messageContext) },
				{ "Text", part.FormatOrderNoteText() }
			};

			PublishModelPartCreatedEvent<OrderNote>(part, m);

			return m;
		}

		protected virtual object CreateModelPart(ShoppingCartItem part, MessageContext messageContext)
		{
			Guard.NotNull(messageContext, nameof(messageContext));
			Guard.NotNull(part, nameof(part));

			var m = new Dictionary<string, object>
			{
				{ "Id", part.Id },
				{ "Quantity", part.Quantity },
				{ "Product", CreateModelPart(part.Product, messageContext, part.AttributesXml) },
			};

			PublishModelPartCreatedEvent<ShoppingCartItem>(part, m);

			return m;
		}

		protected virtual object CreateModelPart(Shipment part, MessageContext messageContext)
		{
			Guard.NotNull(messageContext, nameof(messageContext));
			Guard.NotNull(part, nameof(part));

			var orderItems = new List<OrderItem>();
			var orderItemIds = part.ShipmentItems.Select(x => x.OrderItemId).ToArray();
			foreach (var orderItemId in orderItemIds)
			{
				var orderItem = _services.Resolve<IOrderService>().GetOrderItemById(orderItemId);
				if (orderItem != null)
				{
					orderItems.Add(orderItem);
				}
			}

			var m = new Dictionary<string, object>
			{
				{ "Id", part.Id },
				{ "TrackingNumber", part.TrackingNumber },
				{ "TotalWeight", part.TotalWeight },
				{ "CreatedOn", ToUserDate(part.CreatedOnUtc, messageContext) },
				{ "DeliveredOn", ToUserDate(part.DeliveryDateUtc, messageContext) },
				{ "ShippedOn", ToUserDate(part.ShippedDateUtc, messageContext) },
				{ "Url", BuildActionUrl("ShipmentDetails", "Order", new { id = part.Id, area = "" }, messageContext)},
				{ "OrderItems", orderItems.Where(x => x.Product != null).Select(x => CreateModelPart(x, messageContext)).ToList() },
			};

			PublishModelPartCreatedEvent<Shipment>(part, m);

			return m;
		}

		protected virtual object CreateModelPart(RecurringPayment part, MessageContext messageContext)
		{
			Guard.NotNull(messageContext, nameof(messageContext));
			Guard.NotNull(part, nameof(part));

			var m = new Dictionary<string, object>
			{
				{ "Id", part.Id },
				{ "CreatedOn", ToUserDate(part.CreatedOnUtc, messageContext) },
				{ "StartedOn", ToUserDate(part.StartDateUtc, messageContext) },
				{ "NextOn", ToUserDate(part.NextPaymentDate, messageContext) },
				{ "CycleLength", part.CycleLength },
				{ "CyclePeriod", part.CyclePeriod.GetLocalizedEnum(_services.Localization, messageContext.Language.Id) },
				{ "CyclesRemaining", part.CyclesRemaining },
				{ "TotalCycles", part.TotalCycles }
			};

			PublishModelPartCreatedEvent<RecurringPayment>(part, m);

			return m;
		}

		protected virtual object CreateModelPart(ReturnRequest part, MessageContext messageContext)
		{
			Guard.NotNull(messageContext, nameof(messageContext));
			Guard.NotNull(part, nameof(part));

			var m = new Dictionary<string, object>
			{
				{ "Id", part.Id },
				{ "Reason", part.ReasonForReturn },
				{ "Status", part.ReturnRequestStatus.GetLocalizedEnum(_services.Localization, messageContext.Language.Id) },
				{ "RequestedAction", part.RequestedAction },
				{ "CustomerComments", HtmlUtils.FormatText(part.CustomerComments, true, false, false, false, false, false) },
				{ "StaffNotes", HtmlUtils.FormatText(part.StaffNotes, true, false, false, false, false, false) },
				{ "Quantity", part.Quantity }
			};

			PublishModelPartCreatedEvent<ReturnRequest>(part, m);

			return m;
		}
	}
}