using ReadySignOn.ReadyPay.Models;
using SmartStore;
using SmartStore.Core;
using SmartStore.Core.Domain.Catalog;
using SmartStore.Core.Domain.Common;
using SmartStore.Core.Domain.Customers;
using SmartStore.Core.Domain.Directory;
using SmartStore.Core.Domain.Discounts;
using SmartStore.Core.Domain.Localization;
using SmartStore.Core.Domain.Orders;
using SmartStore.Core.Domain.Payments;
using SmartStore.Core.Domain.Shipping;
using SmartStore.Core.Domain.Tax;
using SmartStore.Core.Events;
using SmartStore.Core.Localization;
using SmartStore.Core.Logging;
using SmartStore.Core.Plugins;
using SmartStore.Services.Affiliates;
using SmartStore.Services.Catalog;
using SmartStore.Services.Common;
using SmartStore.Services.Customers;
using SmartStore.Services.Directory;
using SmartStore.Services.Discounts;
using SmartStore.Services.Localization;
using SmartStore.Services.Messages;
using SmartStore.Services.Orders;
using SmartStore.Services.Payments;
using SmartStore.Services.Security;
using SmartStore.Services.Shipping;
using SmartStore.Services.Tax;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace ReadySignOn.ReadyPay.Services
{
    public class ReadyPayOrders : IReadyPayOrders
    {
        private readonly IOrderService _orderService;
        private readonly IWebHelper _webHelper;
        private readonly ILocalizationService _localizationService;
        private readonly ILanguageService _languageService;
        private readonly IProductService _productService;
        private readonly IPaymentService _paymentService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IProductAttributeFormatter _productAttributeFormatter;
        private readonly IGiftCardService _giftCardService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ICheckoutAttributeFormatter _checkoutAttributeFormatter;
        private readonly IShippingService _shippingService;
        private readonly IShipmentService _shipmentService;
        private readonly ITaxService _taxService;
        private readonly ICustomerService _customerService;
        private readonly IDiscountService _discountService;
        private readonly IEncryptionService _encryptionService;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly IMessageFactory _messageFactory;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ICurrencyService _currencyService;
        private readonly IAffiliateService _affiliateService;
        private readonly IEventPublisher _eventPublisher;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly INewsLetterSubscriptionService _newsLetterSubscriptionService;

        private readonly PaymentSettings _paymentSettings;
        private readonly RewardPointsSettings _rewardPointsSettings;
        private readonly OrderSettings _orderSettings;
        private readonly TaxSettings _taxSettings;
        private readonly LocalizationSettings _localizationSettings;
        private readonly CurrencySettings _currencySettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly CatalogSettings _catalogSettings;

        public Localizer T { get; set; }
        public ILogger Logger { get; set; }

        public ReadyPayOrders(
                                IOrderService orderService,
                                IWebHelper webHelper,
                                ILocalizationService localizationService,
                                ILanguageService languageService,
                                IProductService productService,
                                IPaymentService paymentService,
                                IOrderTotalCalculationService orderTotalCalculationService,
                                IPriceCalculationService priceCalculationService,
                                IPriceFormatter priceFormatter,
                                IProductAttributeParser productAttributeParser,
                                IProductAttributeFormatter productAttributeFormatter,
                                IGiftCardService giftCardService,
                                IShoppingCartService shoppingCartService,
                                ICheckoutAttributeFormatter checkoutAttributeFormatter,
                                IShippingService shippingService,
                                IShipmentService shipmentService,
                                ITaxService taxService,
                                ICustomerService customerService,
                                IDiscountService discountService,
                                IEncryptionService encryptionService,
                                IWorkContext workContext,
                                IStoreContext storeContext,
                                IMessageFactory messageFactory,
                                ICustomerActivityService customerActivityService,
                                ICurrencyService currencyService,
                                IAffiliateService affiliateService,
                                IEventPublisher eventPublisher,
                                IGenericAttributeService genericAttributeService,
                                INewsLetterSubscriptionService newsLetterSubscriptionService,
                                PaymentSettings paymentSettings,
                                RewardPointsSettings rewardPointsSettings,
                                OrderSettings orderSettings,
                                TaxSettings taxSettings,
                                LocalizationSettings localizationSettings,
                                CurrencySettings currencySettings,
                                ShoppingCartSettings shoppingCartSettings,
                                CatalogSettings catalogSettings)
        {
            _orderService = orderService;
            _webHelper = webHelper;
            _localizationService = localizationService;
            _languageService = languageService;
            _productService = productService;
            _paymentService = paymentService;
            _orderTotalCalculationService = orderTotalCalculationService;
            _priceCalculationService = priceCalculationService;
            _priceFormatter = priceFormatter;
            _productAttributeParser = productAttributeParser;
            _productAttributeFormatter = productAttributeFormatter;
            _giftCardService = giftCardService;
            _shoppingCartService = shoppingCartService;
            _checkoutAttributeFormatter = checkoutAttributeFormatter;
            _workContext = workContext;
            _storeContext = storeContext;
            _messageFactory = messageFactory;
            _shippingService = shippingService;
            _shipmentService = shipmentService;
            _taxService = taxService;
            _customerService = customerService;
            _discountService = discountService;
            _encryptionService = encryptionService;
            _customerActivityService = customerActivityService;
            _currencyService = currencyService;
            _affiliateService = affiliateService;
            _eventPublisher = eventPublisher;
            _genericAttributeService = genericAttributeService;
            _newsLetterSubscriptionService = newsLetterSubscriptionService;
            _paymentSettings = paymentSettings;
            _rewardPointsSettings = rewardPointsSettings;
            _orderSettings = orderSettings;
            _taxSettings = taxSettings;
            _localizationSettings = localizationSettings;
            _currencySettings = currencySettings;
            _shoppingCartSettings = shoppingCartSettings;
            _catalogSettings = catalogSettings;

            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;
        }
        public virtual IList<string> GetOrderPlacementWarnings(ReadyOrderRequest processPaymentRequest)
        {
            Guard.NotNull(processPaymentRequest, nameof(processPaymentRequest));

            var customer = _customerService.GetCustomerById(processPaymentRequest.CustomerId);

            return GetOrderPlacementWarnings(processPaymentRequest, customer, out var cart);
        }

        public virtual IList<string> GetOrderPlacementWarnings(
            ReadyOrderRequest processPaymentRequest,
            Customer customer,
            out IList<OrganizedShoppingCartItem> cart)
        {
            Guard.NotNull(processPaymentRequest, nameof(processPaymentRequest));

            cart = null;

            var warnings = new List<string>();
            var skipPaymentWorkflow = false;
            var isRecurringShoppingCart = false;
            var paymentMethodSystemName = processPaymentRequest.PaymentMethodSystemName;

            if (customer == null)
            {
                warnings.Add(T("Customer.DoesNotExist"));
                return warnings;
            }

            // Check whether guest checkout is allowed.
            if (customer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed)
            {
                warnings.Add(T("Checkout.AnonymousNotAllowed"));
                return warnings;
            }

            if(!processPaymentRequest.IsInPlaceReadyPayOrder)
            {
                if (processPaymentRequest.ShoppingCartItemIds.Any())
                {
                    cart = customer.GetCartItems(ShoppingCartType.ShoppingCart, processPaymentRequest.StoreId)
                        .Where(x => processPaymentRequest.ShoppingCartItemIds.Contains(x.Item.Id))
                        .ToList();
                }
                else
                {
                    cart = customer.GetCartItems(ShoppingCartType.ShoppingCart, processPaymentRequest.StoreId);
                }

                if (!cart.Any())
                {
                    warnings.Add(T("ShoppingCart.CartIsEmpty"));
                    return warnings;
                }

                // Validate the entire shopping cart.
                var cartWarnings = _shoppingCartService.GetShoppingCartWarnings(cart, customer.GetAttribute<string>(SystemCustomerAttributeNames.CheckoutAttributes), true);
                if (cartWarnings.Any())
                {
                    warnings.AddRange(cartWarnings);
                    return warnings;
                }

                // Validate individual cart items.
                foreach (var sci in cart)
                {
                    var sciWarnings = _shoppingCartService.GetShoppingCartItemWarnings(customer, sci.Item.ShoppingCartType,
                        sci.Item.Product, processPaymentRequest.StoreId, sci.Item.AttributesXml,
                        sci.Item.CustomerEnteredPrice, sci.Item.Quantity, false, childItems: sci.ChildItems);

                    if (sciWarnings.Any())
                    {
                        warnings.AddRange(sciWarnings);
                        return warnings;
                    }
                }

                // Min totals validation.
                var minOrderSubtotalAmountOk = ValidateMinOrderSubtotalAmount(cart);
                if (!minOrderSubtotalAmountOk)
                {
                    var minOrderSubtotalAmount = _currencyService.ConvertFromPrimaryStoreCurrency(_orderSettings.MinOrderSubtotalAmount, _workContext.WorkingCurrency);
                    warnings.Add(T("Checkout.MinOrderSubtotalAmount", _priceFormatter.FormatPrice(minOrderSubtotalAmount, true, false)));
                    return warnings;
                }

                var minOrderTotalAmountOk = ValidateMinOrderTotalAmount(cart);
                if (!minOrderTotalAmountOk)
                {
                    var minOrderTotalAmount = _currencyService.ConvertFromPrimaryStoreCurrency(_orderSettings.MinOrderTotalAmount, _workContext.WorkingCurrency);
                    warnings.Add(T("Checkout.MinOrderTotalAmount", _priceFormatter.FormatPrice(minOrderTotalAmount, true, false)));
                    return warnings;
                }

                // Total validations.
                var orderShippingTotalInclTax = _orderTotalCalculationService.GetShoppingCartShippingTotal(cart, true, out var orderShippingTaxRate, out var shippingTotalDiscount);
                var orderShippingTotalExclTax = _orderTotalCalculationService.GetShoppingCartShippingTotal(cart, false);

                //if (!orderShippingTotalInclTax.HasValue || !orderShippingTotalExclTax.HasValue)
                //{
                //    warnings.Add(T("Order.CannotCalculateShippingTotal"));
                //    return warnings;
                //}

                var cartTotal = _orderTotalCalculationService.GetShoppingCartTotal(cart);
                if (!cartTotal.TotalAmount.HasValue)
                {
                    warnings.Add(T("Order.CannotCalculateOrderTotal"));
                    return warnings;
                }

                skipPaymentWorkflow = cartTotal.TotalAmount.Value == decimal.Zero;

                // Address validations.
                if (customer.BillingAddress == null)
                {
                    warnings.Add(T("Order.BillingAddressMissing"));
                }
                else if (!customer.BillingAddress.Email.IsEmail())
                {
                    warnings.Add(T("Common.Error.InvalidEmail"));
                }
                else if (customer.BillingAddress.Country != null && !customer.BillingAddress.Country.AllowsBilling)
                {
                    warnings.Add(T("Order.CountryNotAllowedForBilling", customer.BillingAddress.Country.Name));
                }

                if (cart.RequiresShipping())
                {
                    if (customer.ShippingAddress == null)
                    {
                        warnings.Add(T("Order.ShippingAddressMissing"));
                        throw new SmartException();
                    }
                    else if (!customer.ShippingAddress.Email.IsEmail())
                    {
                        warnings.Add(T("Common.Error.InvalidEmail"));
                    }
                    else if (customer.ShippingAddress.Country != null && !customer.ShippingAddress.Country.AllowsShipping)
                    {
                        warnings.Add(T("Order.CountryNotAllowedForShipping", customer.ShippingAddress.Country.Name));
                    }
                }
            }

            return warnings;
        }

        protected string FormatTaxRates(SortedDictionary<decimal, decimal> taxRates)
        {
            var result = string.Empty;

            foreach (var rate in taxRates)
            {
                result += "{0}:{1};   ".FormatInvariant(
                    rate.Key.ToString(CultureInfo.InvariantCulture),
                    rate.Value.ToString(CultureInfo.InvariantCulture));
            }

            return result;
        }
        public virtual PlaceOrderResult PlaceOrder(
            ReadyOrderRequest processPaymentRequest,
            Dictionary<string, string> extraData)
        {
            // Think about moving functionality of processing recurring orders (after the initial order was placed) to ProcessNextRecurringPayment() method.
            Guard.NotNull(processPaymentRequest, nameof(processPaymentRequest));

            if (processPaymentRequest.OrderGuid == Guid.Empty)
            {
                processPaymentRequest.OrderGuid = Guid.NewGuid();
            }

            var result = new PlaceOrderResult();
            var utcNow = DateTime.UtcNow;

            try
            {
                var customer = _customerService.GetCustomerById(processPaymentRequest.CustomerId);
                
                if (customer.BillingAddress == null)
                {
                    customer.BillingAddress = processPaymentRequest.BillingAddress;
                }

                if (customer.ShippingAddress == null)
                {
                    customer.ShippingAddress = processPaymentRequest.ShippingAddress;
                }

                if (customer.Email == null)
                {
                    customer.Email = processPaymentRequest.ShippingAddress.Email;
                }

                var warnings = GetOrderPlacementWarnings(processPaymentRequest, customer, out var cart);
                if (warnings.Any())
                {
                    result.Errors.AddRange(warnings);
                    Logger.Warn(string.Join(" ", result.Errors));
                    return result;
                }

                #region Order details

                // Affilites.
                var affiliateId = 0;
                var affiliate = _affiliateService.GetAffiliateById(customer.AffiliateId);
                if (affiliate != null && affiliate.Active && !affiliate.Deleted)
                {
                    affiliateId = affiliate.Id;
                }

                // Customer currency.
                var customerCurrencyCode = string.Empty;
                var customerCurrencyRate = decimal.Zero;

                {
                    var currencyTmp = _currencyService.GetCurrencyById(customer.GetAttribute<int>(SystemCustomerAttributeNames.CurrencyId, processPaymentRequest.StoreId));
                    var customerCurrency = (currencyTmp != null && currencyTmp.Published) ? currencyTmp : _workContext.WorkingCurrency;
                    customerCurrencyCode = customerCurrency.CurrencyCode;

                    var primaryStoreCurrency = _storeContext.CurrentStore.PrimaryStoreCurrency;
                    customerCurrencyRate = customerCurrency.Rate / primaryStoreCurrency.Rate;
                }

                // Customer language.
                Language customerLanguage = null;
                {
                    customerLanguage = _languageService.GetLanguageById(customer.GetAttribute<int>(SystemCustomerAttributeNames.LanguageId, processPaymentRequest.StoreId));
                }

                if (customerLanguage == null || !customerLanguage.Published)
                {
                    customerLanguage = _workContext.WorkingLanguage;
                }

                // Tax display type.
                var customerTaxDisplayType = TaxDisplayType.IncludingTax;
                {
                    customerTaxDisplayType = _workContext.GetTaxDisplayTypeFor(customer, processPaymentRequest.StoreId);
                }

                // Checkout attributes.
                string checkoutAttributeDescription, checkoutAttributesXml;
                {
                    checkoutAttributesXml = customer.GetAttribute<string>(SystemCustomerAttributeNames.CheckoutAttributes);
                    checkoutAttributeDescription = _checkoutAttributeFormatter.FormatAttributes(checkoutAttributesXml, customer);
                }

                // Applied discount (used to store discount usage history).
                var appliedDiscounts = new List<Discount>();
                decimal orderSubTotalInclTax, orderSubTotalExclTax;
                decimal orderSubTotalDiscountInclTax = 0, orderSubTotalDiscountExclTax = 0;

                #endregion

                #region Payment workflow

                // Skip payment workflow if order total equals zero.
                var skipPaymentWorkflow = true;

                // Payment workflow.
                {
                    processPaymentRequest.PaymentMethodSystemName = Plugin.SystemName;
                }

                // Recurring or standard shopping cart?
                var isRecurringShoppingCart = false;

                // Process payment.
                ProcessPaymentResult processPaymentResult = null;

                {
                    // Payment is not required.
                    processPaymentResult = new ProcessPaymentResult
                    {
                        NewPaymentStatus = PaymentStatus.Paid
                    };
                }

                #endregion

                if (processPaymentResult.Success)
                {
                    // Save order.
                    // Uncomment this line to support transactions.
                    //using (var scope = new System.Transactions.TransactionScope())
                    {
                        #region Save order details

                        var shippingStatus = ShippingStatus.NotYetShipped;

                        var order = new Order
                        {
                            StoreId = processPaymentRequest.StoreId,
                            OrderGuid = processPaymentRequest.OrderGuid,
                            CustomerId = customer.Id,
                            CustomerLanguageId = customerLanguage.Id,
                            CustomerTaxDisplayType = customerTaxDisplayType,
                            CustomerIp = _webHelper.GetCurrentIpAddress(),
                            OrderSubtotalInclTax = processPaymentRequest.OrderSubtotalInclTax,
                            OrderSubtotalExclTax = processPaymentRequest.OrderSubtotalExclTax,
                            OrderSubTotalDiscountInclTax = orderSubTotalDiscountInclTax,
                            OrderSubTotalDiscountExclTax = orderSubTotalDiscountExclTax,
                            OrderShippingInclTax = processPaymentRequest.OrderShippingInclTax,
                            OrderShippingExclTax = processPaymentRequest.OrderShippingExclTax,
                            OrderShippingTaxRate = processPaymentRequest.OrderShippingTaxRate,
                            PaymentMethodAdditionalFeeInclTax = processPaymentRequest.PaymentMethodAdditionalFeeTaxRate,
                            PaymentMethodAdditionalFeeExclTax = processPaymentRequest.PaymentMethodAdditionalFeeExclTax,
                            PaymentMethodAdditionalFeeTaxRate = processPaymentRequest.PaymentMethodAdditionalFeeTaxRate,
                            TaxRates = processPaymentRequest.TaxRates,
                            OrderTax = processPaymentRequest.OrderTax,
                            OrderTotalRounding = processPaymentRequest.OrderTotalRounding,
                            OrderTotal = processPaymentRequest.OrderTotal,
                            RefundedAmount = decimal.Zero,
                            OrderDiscount = processPaymentRequest.OrderDiscount,
                            CreditBalance = processPaymentRequest.CreditBalance,
                            CheckoutAttributeDescription = checkoutAttributeDescription,
                            CheckoutAttributesXml = checkoutAttributesXml,
                            CustomerCurrencyCode = customerCurrencyCode,
                            CurrencyRate = customerCurrencyRate,
                            AffiliateId = affiliateId,
                            OrderStatus = OrderStatus.Pending,
                            AllowStoringCreditCardNumber = processPaymentResult.AllowStoringCreditCardNumber,
                            CardType = processPaymentResult.AllowStoringCreditCardNumber ? _encryptionService.EncryptText(processPaymentRequest.CreditCardType) : string.Empty,
                            CardName = processPaymentResult.AllowStoringCreditCardNumber ? _encryptionService.EncryptText(processPaymentRequest.CreditCardName) : string.Empty,
                            CardNumber = processPaymentResult.AllowStoringCreditCardNumber ? _encryptionService.EncryptText(processPaymentRequest.CreditCardNumber) : string.Empty,
                            MaskedCreditCardNumber = _encryptionService.EncryptText(_paymentService.GetMaskedCreditCardNumber(processPaymentRequest.CreditCardNumber)),
                            CardCvv2 = processPaymentResult.AllowStoringCreditCardNumber ? _encryptionService.EncryptText(processPaymentRequest.CreditCardCvv2) : string.Empty,
                            CardExpirationMonth = processPaymentResult.AllowStoringCreditCardNumber ? _encryptionService.EncryptText(processPaymentRequest.CreditCardExpireMonth.ToString()) : string.Empty,
                            CardExpirationYear = processPaymentResult.AllowStoringCreditCardNumber ? _encryptionService.EncryptText(processPaymentRequest.CreditCardExpireYear.ToString()) : string.Empty,
                            AllowStoringDirectDebit = processPaymentResult.AllowStoringDirectDebit,
                            DirectDebitAccountHolder = processPaymentResult.AllowStoringDirectDebit ? _encryptionService.EncryptText(processPaymentRequest.DirectDebitAccountHolder) : string.Empty,
                            DirectDebitAccountNumber = processPaymentResult.AllowStoringDirectDebit ? _encryptionService.EncryptText(processPaymentRequest.DirectDebitAccountNumber) : string.Empty,
                            DirectDebitBankCode = processPaymentResult.AllowStoringDirectDebit ? _encryptionService.EncryptText(processPaymentRequest.DirectDebitBankCode) : string.Empty,
                            DirectDebitBankName = processPaymentResult.AllowStoringDirectDebit ? _encryptionService.EncryptText(processPaymentRequest.DirectDebitBankName) : string.Empty,
                            DirectDebitBIC = processPaymentResult.AllowStoringDirectDebit ? _encryptionService.EncryptText(processPaymentRequest.DirectDebitBic) : string.Empty,
                            DirectDebitCountry = processPaymentResult.AllowStoringDirectDebit ? _encryptionService.EncryptText(processPaymentRequest.DirectDebitCountry) : string.Empty,
                            DirectDebitIban = processPaymentResult.AllowStoringDirectDebit ? _encryptionService.EncryptText(processPaymentRequest.DirectDebitIban) : string.Empty,
                            PaymentMethodSystemName = processPaymentRequest.PaymentMethodSystemName,
                            AuthorizationTransactionId = processPaymentResult.AuthorizationTransactionId,
                            AuthorizationTransactionCode = processPaymentResult.AuthorizationTransactionCode,
                            AuthorizationTransactionResult = processPaymentResult.AuthorizationTransactionResult,
                            CaptureTransactionId = processPaymentResult.CaptureTransactionId,
                            CaptureTransactionResult = processPaymentResult.CaptureTransactionResult,
                            SubscriptionTransactionId = processPaymentResult.SubscriptionTransactionId,
                            PurchaseOrderNumber = processPaymentRequest.PurchaseOrderNumber,
                            PaymentStatus = processPaymentResult.NewPaymentStatus,
                            PaidDateUtc = null,
                            BillingAddress = processPaymentRequest.BillingAddress,
                            ShippingAddress = processPaymentRequest.ShippingAddress,
                            ShippingStatus = shippingStatus,
                            ShippingMethod = processPaymentRequest.ShippingMethod,
                            ShippingRateComputationMethodSystemName = processPaymentRequest.ShippingRateComputationMethodSystemName,
                            VatNumber = processPaymentRequest.VatNumber,
                            CustomerOrderComment = extraData.ContainsKey("CustomerComment") ? extraData["CustomerComment"] : ""
                        };

                        if (extraData.ContainsKey("AcceptThirdPartyEmailHandOver") && _shoppingCartSettings.ThirdPartyEmailHandOver != CheckoutThirdPartyEmailHandOver.None)
                        {
                            order.AcceptThirdPartyEmailHandOver = extraData["AcceptThirdPartyEmailHandOver"].ToBool();
                        }

                        _orderService.InsertOrder(order);

                        result.PlacedOrder = order;

                        if(cart != null && cart.Count() > 0)
                        {
                            // Move shopping cart items to order products.
                            foreach (var sc in cart)
                            {
                                sc.Item.Product.MergeWithCombination(sc.Item.AttributesXml);

                                // Prices.
                                decimal taxRate = decimal.Zero;
                                decimal unitPriceTaxRate = decimal.Zero;
                                decimal scUnitPrice = _priceCalculationService.GetUnitPrice(sc, true);
                                decimal scSubTotal = _priceCalculationService.GetSubTotal(sc, true);
                                decimal scUnitPriceInclTax = _taxService.GetProductPrice(sc.Item.Product, scUnitPrice, true, customer, out unitPriceTaxRate);
                                decimal scUnitPriceExclTax = _taxService.GetProductPrice(sc.Item.Product, scUnitPrice, false, customer, out taxRate);
                                decimal scSubTotalInclTax = _taxService.GetProductPrice(sc.Item.Product, scSubTotal, true, customer, out taxRate);
                                decimal scSubTotalExclTax = _taxService.GetProductPrice(sc.Item.Product, scSubTotal, false, customer, out taxRate);

                                // Discounts.
                                Discount scDiscount = null;
                                decimal discountAmount = _priceCalculationService.GetDiscountAmount(sc, out scDiscount);
                                decimal discountAmountInclTax = _taxService.GetProductPrice(sc.Item.Product, discountAmount, true, customer, out taxRate);
                                decimal discountAmountExclTax = _taxService.GetProductPrice(sc.Item.Product, discountAmount, false, customer, out taxRate);

                                if (scDiscount != null && !appliedDiscounts.Any(x => x.Id == scDiscount.Id))
                                {
                                    appliedDiscounts.Add(scDiscount);
                                }

                                var attributeDescription = _productAttributeFormatter.FormatAttributes(sc.Item.Product, sc.Item.AttributesXml, customer);
                                var itemWeight = _shippingService.GetShoppingCartItemWeight(sc);
                                var displayDeliveryTime =
                                    _shoppingCartSettings.ShowDeliveryTimes &&
                                    sc.Item.Product.DeliveryTimeId.HasValue &&
                                    sc.Item.Product.IsShipEnabled &&
                                    sc.Item.Product.DisplayDeliveryTimeAccordingToStock(_catalogSettings);

                                // Save order item.
                                var orderItem = new OrderItem
                                {
                                    OrderItemGuid = Guid.NewGuid(),
                                    Order = order,
                                    ProductId = sc.Item.ProductId,
                                    UnitPriceInclTax = scUnitPriceInclTax,
                                    UnitPriceExclTax = scUnitPriceExclTax,
                                    PriceInclTax = scSubTotalInclTax,
                                    PriceExclTax = scSubTotalExclTax,
                                    TaxRate = unitPriceTaxRate,
                                    AttributeDescription = attributeDescription,
                                    AttributesXml = sc.Item.AttributesXml,
                                    Quantity = sc.Item.Quantity,
                                    DiscountAmountInclTax = discountAmountInclTax,
                                    DiscountAmountExclTax = discountAmountExclTax,
                                    DownloadCount = 0,
                                    IsDownloadActivated = false,
                                    LicenseDownloadId = 0,
                                    ItemWeight = itemWeight,
                                    ProductCost = _priceCalculationService.GetProductCost(sc.Item.Product, sc.Item.AttributesXml),
                                    DeliveryTimeId = sc.Item.Product.GetDeliveryTimeIdAccordingToStock(_catalogSettings),
                                    DisplayDeliveryTime = displayDeliveryTime
                                };

                                if (sc.Item.Product.ProductType == ProductType.BundledProduct && sc.ChildItems != null)
                                {
                                    var listBundleData = new List<ProductBundleItemOrderData>();

                                    foreach (var childItem in sc.ChildItems)
                                    {
                                        var bundleItemSubTotal = _taxService.GetProductPrice(childItem.Item.Product, _priceCalculationService.GetSubTotal(childItem, true), out taxRate);

                                        var attributesInfo = _productAttributeFormatter.FormatAttributes(childItem.Item.Product, childItem.Item.AttributesXml, order.Customer,
                                            renderPrices: false, allowHyperlinks: true);

                                        childItem.BundleItemData.ToOrderData(listBundleData, bundleItemSubTotal, childItem.Item.AttributesXml, attributesInfo);
                                    }

                                    orderItem.SetBundleData(listBundleData);
                                }

                                order.OrderItems.Add(orderItem);
                                _orderService.UpdateOrder(order);

                                // Gift cards.
                                if (sc.Item.Product.IsGiftCard)
                                {
                                    _productAttributeParser.GetGiftCardAttribute(
                                        sc.Item.AttributesXml,
                                        out var giftCardRecipientName,
                                        out var giftCardRecipientEmail,
                                        out var giftCardSenderName,
                                        out var giftCardSenderEmail,
                                        out var giftCardMessage);

                                    for (int i = 0; i < sc.Item.Quantity; i++)
                                    {
                                        var gc = new GiftCard
                                        {
                                            GiftCardType = sc.Item.Product.GiftCardType,
                                            PurchasedWithOrderItem = orderItem,
                                            Amount = scUnitPriceExclTax,
                                            IsGiftCardActivated = false,
                                            GiftCardCouponCode = _giftCardService.GenerateGiftCardCode(),
                                            RecipientName = giftCardRecipientName,
                                            RecipientEmail = giftCardRecipientEmail,
                                            SenderName = giftCardSenderName,
                                            SenderEmail = giftCardSenderEmail,
                                            Message = giftCardMessage,
                                            IsRecipientNotified = false,
                                            CreatedOnUtc = utcNow
                                        };
                                        _giftCardService.InsertGiftCard(gc);
                                    }
                                }

                                _productService.AdjustInventory(sc, true);
                            }

                            // Clear shopping cart.
                            if (!processPaymentRequest.IsMultiOrder)
                            {
                                cart.ToList().ForEach(sci => _shoppingCartService.DeleteShoppingCartItem(sci.Item, false));
                            }
                        }

                        // Discount usage history.
                        {
                            foreach (var discount in appliedDiscounts)
                            {
                                var duh = new DiscountUsageHistory
                                {
                                    Discount = discount,
                                    Order = order,
                                    CreatedOnUtc = utcNow
                                };
                                _discountService.InsertDiscountUsageHistory(duh);
                            }
                        }

                        #endregion

                        #region Notifications, notes and attributes

                        // Notes, messages.
                        _orderService.AddOrderNote(order, T("Admin.OrderNotice.OrderPlaced"));

                        // Send email notifications.
                        var msg = _messageFactory.SendOrderPlacedStoreOwnerNotification(order, _localizationSettings.DefaultAdminLanguageId);
                        if (msg?.Email?.Id != null)
                        {
                            _orderService.AddOrderNote(order, T("Admin.OrderNotice.MerchantEmailQueued", msg.Email.Id));
                        }

                        if (string.IsNullOrWhiteSpace(order.Customer.Email) )
                        {
                            order.Customer.Email = order.BillingAddress.Email ?? order.ShippingAddress.Email;
                        }

                        msg = _messageFactory.SendOrderPlacedCustomerNotification(order, order.CustomerLanguageId);
                        if (msg?.Email?.Id != null)
                        {
                            _orderService.AddOrderNote(order, T("Admin.OrderNotice.CustomerEmailQueued", msg.Email.Id));
                        }

                        // Check order status.
                        CheckOrderStatus(order);

                        // Reset checkout data.
                        if (!processPaymentRequest.IsMultiOrder)
                        {
                            _customerService.ResetCheckoutData(customer, processPaymentRequest.StoreId, true, true, true, clearCreditBalance: true);
                        }

                        // Check for generic attributes to be inserted automatically.
                        foreach (var customProperty in processPaymentRequest.CustomProperties.Where(x => x.Key.HasValue() && x.Value.AutoCreateGenericAttribute))
                        {
                            _genericAttributeService.SaveAttribute<object>(order, customProperty.Key, customProperty.Value.Value, order.StoreId);
                        }

                        // Uncomment this line to support transactions.
                        //scope.Complete();

                        // Publish events.
                        _eventPublisher.PublishOrderPlaced(order);

                        {
                            _customerActivityService.InsertActivity("PublicStore.PlaceOrder", T("ActivityLog.PublicStore.PlaceOrder", order.GetOrderNumber()));
                        }

                        if (order.PaymentStatus == PaymentStatus.Paid)
                        {
                            _eventPublisher.PublishOrderPaid(order);
                        }

                        #endregion

                        #region Newsletter subscription

                        if (extraData.ContainsKey("SubscribeToNewsLetter") && _shoppingCartSettings.NewsLetterSubscription != CheckoutNewsLetterSubscription.None)
                        {
                            var addSubscription = extraData["SubscribeToNewsLetter"].ToBool();

                            bool? nsResult = _newsLetterSubscriptionService.AddNewsLetterSubscriptionFor(addSubscription, customer.Email, order.StoreId);

                            if (nsResult.HasValue)
                            {
                                _orderService.AddOrderNote(order, T(nsResult.Value ? "Admin.OrderNotice.NewsLetterSubscriptionAdded" : "Admin.OrderNotice.NewsLetterSubscriptionRemoved"));
                            }
                        }

                        #endregion
                    }
                }
                else
                {
                    result.AddError(T("Payment.PayingFailed"));

                    foreach (var paymentError in processPaymentResult.Errors)
                    {
                        result.AddError(paymentError);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                result.AddError(ex.Message);
            }

            if (result.Errors.Any())
            {
                Logger.Error(string.Join(" ", result.Errors));
            }

            return result;
        }

        /// <summary>
        /// Checks order status
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Validated order</returns>
        public void CheckOrderStatus(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            if (order.PaymentStatus == PaymentStatus.Paid && !order.PaidDateUtc.HasValue)
            {
                //ensure that paid date is set
                order.PaidDateUtc = DateTime.UtcNow;
                _orderService.UpdateOrder(order);
            }

            if (order.OrderStatus == OrderStatus.Pending)
            {
                if (order.PaymentStatus == PaymentStatus.Authorized || order.PaymentStatus == PaymentStatus.Paid)
                {
                    SetOrderStatus(order, OrderStatus.Processing, false);
                }
            }

            if (order.OrderStatus == OrderStatus.Pending)
            {
                if (order.ShippingStatus == ShippingStatus.PartiallyShipped || order.ShippingStatus == ShippingStatus.Shipped || order.ShippingStatus == ShippingStatus.Delivered)
                {
                    SetOrderStatus(order, OrderStatus.Processing, false);
                }
            }

            if (order.OrderStatus != OrderStatus.Cancelled && order.OrderStatus != OrderStatus.Complete)
            {
                if (order.PaymentStatus == PaymentStatus.Paid)
                {
                    if (order.ShippingStatus == ShippingStatus.ShippingNotRequired || order.ShippingStatus == ShippingStatus.Delivered)
                    {
                        SetOrderStatus(order, OrderStatus.Complete, true);
                    }
                }
            }
        }

        /// <summary>
        /// Sets an order status
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="os">New order status</param>
        /// <param name="notifyCustomer">True to notify customer</param>
        protected void SetOrderStatus(Order order, OrderStatus os, bool notifyCustomer)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            OrderStatus prevOrderStatus = order.OrderStatus;
            if (prevOrderStatus == os)
                return;

            //set and save new order status
            order.OrderStatusId = (int)os;
            _orderService.UpdateOrder(order);

            _orderService.AddOrderNote(order, T("Admin.OrderNotice.OrderStatusChanged", os.GetLocalizedEnum(_localizationService)));

            if (prevOrderStatus != OrderStatus.Complete && os == OrderStatus.Complete && notifyCustomer)
            {
                //notification
                var msg = _messageFactory.SendOrderCompletedCustomerNotification(order, order.CustomerLanguageId);
                if (msg?.Email?.Id != null)
                {
                    _orderService.AddOrderNote(order, T("Admin.OrderNotice.CustomerCompletedEmailQueued", msg.Email.Id));
                }
            }

            if (prevOrderStatus != OrderStatus.Cancelled && os == OrderStatus.Cancelled && notifyCustomer)
            {
                //notification
                var msg = _messageFactory.SendOrderCancelledCustomerNotification(order, order.CustomerLanguageId);
                if (msg?.Email?.Id != null)
                {
                    _orderService.AddOrderNote(order, T("Admin.OrderNotice.CustomerCancelledEmailQueued", msg.Email.Id));
                }
            }
        }

        /// <summary>
        /// Valdiate minimum order sub-total amount
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>true - OK; false - minimum order sub-total amount is not reached</returns>
        public virtual bool ValidateMinOrderSubtotalAmount(IList<OrganizedShoppingCartItem> cart)
        {
            if (cart == null)
                throw new ArgumentNullException("cart");

            //min order amount sub-total validation
            if (cart.Count > 0 && _orderSettings.MinOrderSubtotalAmount > decimal.Zero)
            {
                decimal orderSubTotalDiscountAmountBase = decimal.Zero;
                Discount orderSubTotalAppliedDiscount = null;
                decimal subTotalWithoutDiscountBase = decimal.Zero;
                decimal subTotalWithDiscountBase = decimal.Zero;

                _orderTotalCalculationService.GetShoppingCartSubTotal(cart,
                    out orderSubTotalDiscountAmountBase, out orderSubTotalAppliedDiscount, out subTotalWithoutDiscountBase, out subTotalWithDiscountBase);

                if (subTotalWithoutDiscountBase < _orderSettings.MinOrderSubtotalAmount)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Valdiate minimum order total amount
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>true - OK; false - minimum order total amount is not reached</returns>
		public virtual bool ValidateMinOrderTotalAmount(IList<OrganizedShoppingCartItem> cart)
        {
            if (cart == null)
                throw new ArgumentNullException("cart");

            if (cart.Count > 0 && _orderSettings.MinOrderTotalAmount > decimal.Zero)
            {
                decimal? shoppingCartTotalBase = _orderTotalCalculationService.GetShoppingCartTotal(cart);

                if (shoppingCartTotalBase.HasValue && shoppingCartTotalBase.Value < _orderSettings.MinOrderTotalAmount)
                    return false;
            }

            return true;
        }
    }
}