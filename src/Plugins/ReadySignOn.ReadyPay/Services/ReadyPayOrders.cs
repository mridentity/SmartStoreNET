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
    public class ReadyPayOrders
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
        public virtual IList<string> GetOrderPlacementWarnings(ProcessPaymentRequest processPaymentRequest)
        {
            Guard.NotNull(processPaymentRequest, nameof(processPaymentRequest));

            var initialOrder = _orderService.GetOrderById(processPaymentRequest.InitialOrderId);
            var customer = _customerService.GetCustomerById(processPaymentRequest.CustomerId);

            return GetOrderPlacementWarnings(processPaymentRequest, initialOrder, customer, out var cart);
        }

        public virtual IList<string> GetOrderPlacementWarnings(
            ProcessPaymentRequest processPaymentRequest,
            Order initialOrder,
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

            if (!processPaymentRequest.IsRecurringPayment)
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
            ProcessPaymentRequest processPaymentRequest,
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
                var initialOrder = _orderService.GetOrderById(processPaymentRequest.InitialOrderId);
                var customer = _customerService.GetCustomerById(processPaymentRequest.CustomerId);

                var warnings = GetOrderPlacementWarnings(processPaymentRequest, initialOrder, customer, out var cart);
                if (warnings.Any())
                {
                    result.Errors.AddRange(warnings);
                    Logger.Warn(string.Join(" ", result.Errors));
                    return result;
                }

                #region Order details

                if (processPaymentRequest.IsRecurringPayment)
                {
                    processPaymentRequest.PaymentMethodSystemName = initialOrder.PaymentMethodSystemName;
                }

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
                if (!processPaymentRequest.IsRecurringPayment)
                {
                    var currencyTmp = _currencyService.GetCurrencyById(customer.GetAttribute<int>(SystemCustomerAttributeNames.CurrencyId, processPaymentRequest.StoreId));
                    var customerCurrency = (currencyTmp != null && currencyTmp.Published) ? currencyTmp : _workContext.WorkingCurrency;
                    customerCurrencyCode = customerCurrency.CurrencyCode;

                    var primaryStoreCurrency = _storeContext.CurrentStore.PrimaryStoreCurrency;
                    customerCurrencyRate = customerCurrency.Rate / primaryStoreCurrency.Rate;
                }
                else
                {
                    customerCurrencyCode = initialOrder.CustomerCurrencyCode;
                    customerCurrencyRate = initialOrder.CurrencyRate;
                }

                // Customer language.
                Language customerLanguage = null;
                if (!processPaymentRequest.IsRecurringPayment)
                {
                    customerLanguage = _languageService.GetLanguageById(customer.GetAttribute<int>(SystemCustomerAttributeNames.LanguageId, processPaymentRequest.StoreId));
                }
                else
                {
                    customerLanguage = _languageService.GetLanguageById(initialOrder.CustomerLanguageId);
                }

                if (customerLanguage == null || !customerLanguage.Published)
                {
                    customerLanguage = _workContext.WorkingLanguage;
                }

                // Tax display type.
                var customerTaxDisplayType = TaxDisplayType.IncludingTax;
                if (!processPaymentRequest.IsRecurringPayment)
                {
                    customerTaxDisplayType = _workContext.GetTaxDisplayTypeFor(customer, processPaymentRequest.StoreId);
                }
                else
                {
                    customerTaxDisplayType = initialOrder.CustomerTaxDisplayType;
                }

                // Checkout attributes.
                string checkoutAttributeDescription, checkoutAttributesXml;
                if (!processPaymentRequest.IsRecurringPayment)
                {
                    checkoutAttributesXml = customer.GetAttribute<string>(SystemCustomerAttributeNames.CheckoutAttributes);
                    checkoutAttributeDescription = _checkoutAttributeFormatter.FormatAttributes(checkoutAttributesXml, customer);
                }
                else
                {
                    checkoutAttributesXml = initialOrder.CheckoutAttributesXml;
                    checkoutAttributeDescription = initialOrder.CheckoutAttributeDescription;
                }

                // Applied discount (used to store discount usage history).
                var appliedDiscounts = new List<Discount>();
                decimal orderSubTotalInclTax, orderSubTotalExclTax;
                decimal orderSubTotalDiscountInclTax = 0, orderSubTotalDiscountExclTax = 0;
                if (!processPaymentRequest.IsRecurringPayment)
                {
                    // Sub total (incl tax).
                    _orderTotalCalculationService.GetShoppingCartSubTotal(cart, true,
                        out var orderSubTotalDiscountAmount1, out var orderSubTotalAppliedDiscount1, out var subTotalWithoutDiscountBase1, out var subTotalWithDiscountBase1);

                    orderSubTotalInclTax = subTotalWithoutDiscountBase1;
                    orderSubTotalDiscountInclTax = orderSubTotalDiscountAmount1;

                    // Discount history.
                    if (orderSubTotalAppliedDiscount1 != null && !appliedDiscounts.Any(x => x.Id == orderSubTotalAppliedDiscount1.Id))
                    {
                        appliedDiscounts.Add(orderSubTotalAppliedDiscount1);
                    }

                    // Sub total (excl tax).
                    _orderTotalCalculationService.GetShoppingCartSubTotal(cart, false,
                        out var orderSubTotalDiscountAmount2, out var orderSubTotalAppliedDiscount2, out var subTotalWithoutDiscountBase2, out var subTotalWithDiscountBase2);

                    orderSubTotalExclTax = subTotalWithoutDiscountBase2;
                    orderSubTotalDiscountExclTax = orderSubTotalDiscountAmount2;
                }
                else
                {
                    orderSubTotalInclTax = initialOrder.OrderSubtotalInclTax;
                    orderSubTotalExclTax = initialOrder.OrderSubtotalExclTax;
                }


                // Shipping info.
                var shoppingCartRequiresShipping = false;
                if (!processPaymentRequest.IsRecurringPayment)
                {
                    shoppingCartRequiresShipping = cart.RequiresShipping();
                }
                else
                {
                    shoppingCartRequiresShipping = initialOrder.ShippingStatus != ShippingStatus.ShippingNotRequired;
                }

                string shippingMethodName = "", shippingRateComputationMethodSystemName = "";
                if (shoppingCartRequiresShipping)
                {
                    if (!processPaymentRequest.IsRecurringPayment)
                    {
                        var shippingOption = customer.GetAttribute<ShippingOption>(SystemCustomerAttributeNames.SelectedShippingOption, processPaymentRequest.StoreId);
                        if (shippingOption != null)
                        {
                            shippingMethodName = shippingOption.Name;
                            shippingRateComputationMethodSystemName = shippingOption.ShippingRateComputationMethodSystemName;
                        }
                    }
                    else
                    {
                        shippingMethodName = initialOrder.ShippingMethod;
                        shippingRateComputationMethodSystemName = initialOrder.ShippingRateComputationMethodSystemName;
                    }
                }

                // Shipping total.
                decimal? orderShippingTotalInclTax, orderShippingTotalExclTax = null;
                decimal orderShippingTaxRate = decimal.Zero;
                if (!processPaymentRequest.IsRecurringPayment)
                {
                    orderShippingTotalInclTax = _orderTotalCalculationService.GetShoppingCartShippingTotal(cart, true, out orderShippingTaxRate, out var shippingTotalDiscount);
                    orderShippingTotalExclTax = _orderTotalCalculationService.GetShoppingCartShippingTotal(cart, false);

                    if (shippingTotalDiscount != null && !appliedDiscounts.Any(x => x.Id == shippingTotalDiscount.Id))
                    {
                        appliedDiscounts.Add(shippingTotalDiscount);
                    }
                }
                else
                {
                    orderShippingTotalInclTax = initialOrder.OrderShippingInclTax;
                    orderShippingTotalExclTax = initialOrder.OrderShippingExclTax;
                    orderShippingTaxRate = initialOrder.OrderShippingTaxRate;
                }

                // Payment total.
                decimal paymentAdditionalFeeInclTax, paymentAdditionalFeeExclTax, paymentAdditionalFeeTaxRate;
                if (!processPaymentRequest.IsRecurringPayment)
                {
                    var paymentAdditionalFee = _paymentService.GetAdditionalHandlingFee(cart, processPaymentRequest.PaymentMethodSystemName);
                    paymentAdditionalFeeInclTax = _taxService.GetPaymentMethodAdditionalFee(paymentAdditionalFee, true, customer, out paymentAdditionalFeeTaxRate);
                    paymentAdditionalFeeExclTax = _taxService.GetPaymentMethodAdditionalFee(paymentAdditionalFee, false, customer);
                }
                else
                {
                    paymentAdditionalFeeInclTax = initialOrder.PaymentMethodAdditionalFeeInclTax;
                    paymentAdditionalFeeExclTax = initialOrder.PaymentMethodAdditionalFeeExclTax;
                    paymentAdditionalFeeTaxRate = initialOrder.PaymentMethodAdditionalFeeTaxRate;
                }

                // Tax total.
                var orderTaxTotal = decimal.Zero;
                string vatNumber = "", taxRates = "";
                if (!processPaymentRequest.IsRecurringPayment)
                {
                    // Tax amount.
                    orderTaxTotal = _orderTotalCalculationService.GetTaxTotal(cart, out var taxRatesDictionary);

                    // VAT number.
                    var customerVatStatus = (VatNumberStatus)customer.VatNumberStatusId;
                    if (_taxSettings.EuVatEnabled && customerVatStatus == VatNumberStatus.Valid)
                    {
                        vatNumber = customer.GetAttribute<string>(SystemCustomerAttributeNames.VatNumber);
                    }

                    taxRates = FormatTaxRates(taxRatesDictionary);
                }
                else
                {
                    orderTaxTotal = initialOrder.OrderTax;
                    vatNumber = initialOrder.VatNumber;
                }

                processPaymentRequest.OrderTax = orderTaxTotal;

                // Order total (and applied discounts, gift cards, reward points).
                ShoppingCartTotal cartTotal = null;

                if (!processPaymentRequest.IsRecurringPayment)
                {
                    cartTotal = _orderTotalCalculationService.GetShoppingCartTotal(cart);

                    // Discount history.
                    if (cartTotal.AppliedDiscount != null && !appliedDiscounts.Any(x => x.Id == cartTotal.AppliedDiscount.Id))
                    {
                        appliedDiscounts.Add(cartTotal.AppliedDiscount);
                    }
                }
                else
                {
                    cartTotal = new ShoppingCartTotal(initialOrder.OrderTotal);
                    cartTotal.DiscountAmount = initialOrder.OrderDiscount;
                }

                processPaymentRequest.OrderTotal = cartTotal.TotalAmount.Value;

                #endregion

                #region Addresses & pre-payment workflow

                // Give payment processor the opportunity to fullfill billing address.
                var preProcessPaymentResult = _paymentService.PreProcessPayment(processPaymentRequest);

                if (!preProcessPaymentResult.Success)
                {
                    result.Errors.AddRange(preProcessPaymentResult.Errors);
                    result.Errors.Add(T("Common.Error.PreProcessPayment"));
                    return result;
                }

                var billingAddress = !processPaymentRequest.IsRecurringPayment
                    ? (Address)customer.BillingAddress.Clone()
                    : (Address)initialOrder.BillingAddress.Clone();

                Address shippingAddress = null;
                if (shoppingCartRequiresShipping)
                {
                    shippingAddress = !processPaymentRequest.IsRecurringPayment
                        ? (Address)customer.ShippingAddress.Clone()
                        : (Address)initialOrder.ShippingAddress.Clone();
                }

                #endregion

                #region Payment workflow

                // Skip payment workflow if order total equals zero.
                var skipPaymentWorkflow = cartTotal.TotalAmount.Value == decimal.Zero;

                // Payment workflow.
                Provider<IPaymentMethod> paymentMethod = null;
                if (!skipPaymentWorkflow)
                {
                    paymentMethod = _paymentService.LoadPaymentMethodBySystemName(processPaymentRequest.PaymentMethodSystemName);
                }
                else
                {
                    processPaymentRequest.PaymentMethodSystemName = "";
                }

                // Recurring or standard shopping cart?
                var isRecurringShoppingCart = false;
                if (!processPaymentRequest.IsRecurringPayment)
                {
                    isRecurringShoppingCart = cart.IsRecurring();
                    if (isRecurringShoppingCart)
                    {
                        var unused = cart.GetRecurringCycleInfo(_localizationService, out var recurringCycleLength, out var recurringCyclePeriod, out var recurringTotalCycles);

                        processPaymentRequest.RecurringCycleLength = recurringCycleLength;
                        processPaymentRequest.RecurringCyclePeriod = recurringCyclePeriod;
                        processPaymentRequest.RecurringTotalCycles = recurringTotalCycles;
                    }
                }
                else
                {
                    isRecurringShoppingCart = true;
                }

                // Process payment.
                ProcessPaymentResult processPaymentResult = null;
                if (!skipPaymentWorkflow && !processPaymentRequest.IsMultiOrder)
                {
                    if (!processPaymentRequest.IsRecurringPayment)
                    {
                        if (isRecurringShoppingCart)
                        {
                            // Recurring cart.
                            var recurringPaymentType = _paymentService.GetRecurringPaymentType(processPaymentRequest.PaymentMethodSystemName);
                            switch (recurringPaymentType)
                            {
                                case RecurringPaymentType.NotSupported:
                                    throw new SmartException(T("Payment.RecurringPaymentNotSupported"));
                                case RecurringPaymentType.Manual:
                                case RecurringPaymentType.Automatic:
                                    processPaymentResult = _paymentService.ProcessRecurringPayment(processPaymentRequest);
                                    break;
                                default:
                                    throw new SmartException(T("Payment.RecurringPaymentTypeUnknown"));
                            }
                        }
                        else
                        {
                            // Standard cart.
                            processPaymentResult = _paymentService.ProcessPayment(processPaymentRequest);
                        }
                    }
                    else
                    {
                        if (isRecurringShoppingCart)
                        {
                            // Old credit card info.
                            processPaymentRequest.CreditCardType = initialOrder.AllowStoringCreditCardNumber ? _encryptionService.DecryptText(initialOrder.CardType) : "";
                            processPaymentRequest.CreditCardName = initialOrder.AllowStoringCreditCardNumber ? _encryptionService.DecryptText(initialOrder.CardName) : "";
                            processPaymentRequest.CreditCardNumber = initialOrder.AllowStoringCreditCardNumber ? _encryptionService.DecryptText(initialOrder.CardNumber) : "";
                            // MaskedCreditCardNumber.
                            processPaymentRequest.CreditCardCvv2 = initialOrder.AllowStoringCreditCardNumber ? _encryptionService.DecryptText(initialOrder.CardCvv2) : "";

                            try
                            {
                                processPaymentRequest.CreditCardExpireMonth = initialOrder.AllowStoringCreditCardNumber ? Convert.ToInt32(_encryptionService.DecryptText(initialOrder.CardExpirationMonth)) : 0;
                                processPaymentRequest.CreditCardExpireYear = initialOrder.AllowStoringCreditCardNumber ? Convert.ToInt32(_encryptionService.DecryptText(initialOrder.CardExpirationYear)) : 0;
                            }
                            catch { }

                            var recurringPaymentType = _paymentService.GetRecurringPaymentType(processPaymentRequest.PaymentMethodSystemName);
                            switch (recurringPaymentType)
                            {
                                case RecurringPaymentType.NotSupported:
                                    throw new SmartException(T("Payment.RecurringPaymentNotSupported"));
                                case RecurringPaymentType.Manual:
                                    processPaymentResult = _paymentService.ProcessRecurringPayment(processPaymentRequest);
                                    break;
                                case RecurringPaymentType.Automatic:
                                    // Payment is processed on payment gateway site.
                                    processPaymentResult = new ProcessPaymentResult();
                                    break;
                                default:
                                    throw new SmartException(T("Payment.RecurringPaymentTypeUnknown"));
                            }
                        }
                        else
                        {
                            throw new SmartException(T("Order.NoRecurringProducts"));
                        }
                    }
                }
                else
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
                        if (!shoppingCartRequiresShipping)
                        {
                            shippingStatus = ShippingStatus.ShippingNotRequired;
                        }

                        var order = new Order
                        {
                            StoreId = processPaymentRequest.StoreId,
                            OrderGuid = processPaymentRequest.OrderGuid,
                            CustomerId = customer.Id,
                            CustomerLanguageId = customerLanguage.Id,
                            CustomerTaxDisplayType = customerTaxDisplayType,
                            CustomerIp = _webHelper.GetCurrentIpAddress(),
                            OrderSubtotalInclTax = orderSubTotalInclTax,
                            OrderSubtotalExclTax = orderSubTotalExclTax,
                            OrderSubTotalDiscountInclTax = orderSubTotalDiscountInclTax,
                            OrderSubTotalDiscountExclTax = orderSubTotalDiscountExclTax,
                            OrderShippingInclTax = orderShippingTotalInclTax.Value,
                            OrderShippingExclTax = orderShippingTotalExclTax.Value,
                            OrderShippingTaxRate = orderShippingTaxRate,
                            PaymentMethodAdditionalFeeInclTax = paymentAdditionalFeeInclTax,
                            PaymentMethodAdditionalFeeExclTax = paymentAdditionalFeeExclTax,
                            PaymentMethodAdditionalFeeTaxRate = paymentAdditionalFeeTaxRate,
                            TaxRates = taxRates,
                            OrderTax = orderTaxTotal,
                            OrderTotalRounding = cartTotal.RoundingAmount,
                            OrderTotal = cartTotal.TotalAmount.Value,
                            RefundedAmount = decimal.Zero,
                            OrderDiscount = cartTotal.DiscountAmount,
                            CreditBalance = cartTotal.CreditBalance,
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
                            BillingAddress = billingAddress,
                            ShippingAddress = shippingAddress,
                            ShippingStatus = shippingStatus,
                            ShippingMethod = shippingMethodName,
                            ShippingRateComputationMethodSystemName = shippingRateComputationMethodSystemName,
                            VatNumber = vatNumber,
                            CustomerOrderComment = extraData.ContainsKey("CustomerComment") ? extraData["CustomerComment"] : ""
                        };

                        if (extraData.ContainsKey("AcceptThirdPartyEmailHandOver") && _shoppingCartSettings.ThirdPartyEmailHandOver != CheckoutThirdPartyEmailHandOver.None)
                        {
                            order.AcceptThirdPartyEmailHandOver = extraData["AcceptThirdPartyEmailHandOver"].ToBool();
                        }

                        _orderService.InsertOrder(order);

                        result.PlacedOrder = order;

                        if (!processPaymentRequest.IsRecurringPayment)
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
                        else
                        {
                            // Recurring payment.
                            var initialOrderItems = initialOrder.OrderItems;
                            foreach (var orderItem in initialOrderItems)
                            {
                                // Save item.
                                var newOrderItem = new OrderItem
                                {
                                    OrderItemGuid = Guid.NewGuid(),
                                    Order = order,
                                    ProductId = orderItem.ProductId,
                                    UnitPriceInclTax = orderItem.UnitPriceInclTax,
                                    UnitPriceExclTax = orderItem.UnitPriceExclTax,
                                    PriceInclTax = orderItem.PriceInclTax,
                                    PriceExclTax = orderItem.PriceExclTax,
                                    TaxRate = orderItem.TaxRate,
                                    AttributeDescription = orderItem.AttributeDescription,
                                    AttributesXml = orderItem.AttributesXml,
                                    Quantity = orderItem.Quantity,
                                    DiscountAmountInclTax = orderItem.DiscountAmountInclTax,
                                    DiscountAmountExclTax = orderItem.DiscountAmountExclTax,
                                    DownloadCount = 0,
                                    IsDownloadActivated = false,
                                    LicenseDownloadId = 0,
                                    ItemWeight = orderItem.ItemWeight,
                                    BundleData = orderItem.BundleData,
                                    ProductCost = orderItem.ProductCost,
                                    DeliveryTimeId = orderItem.DeliveryTimeId,
                                    DisplayDeliveryTime = orderItem.DisplayDeliveryTime
                                };
                                order.OrderItems.Add(newOrderItem);
                                _orderService.UpdateOrder(order);

                                // Gift cards.
                                if (orderItem.Product.IsGiftCard)
                                {
                                    _productAttributeParser.GetGiftCardAttribute(
                                        orderItem.AttributesXml,
                                        out var giftCardRecipientName,
                                        out var giftCardRecipientEmail,
                                        out var giftCardSenderName,
                                        out var giftCardSenderEmail,
                                        out var giftCardMessage);

                                    for (int i = 0; i < orderItem.Quantity; i++)
                                    {
                                        var gc = new GiftCard
                                        {
                                            GiftCardType = orderItem.Product.GiftCardType,
                                            PurchasedWithOrderItem = newOrderItem,
                                            Amount = orderItem.UnitPriceExclTax,
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

                                _productService.AdjustInventory(orderItem, true, orderItem.Quantity);
                            }
                        }

                        // Discount usage history.
                        if (!processPaymentRequest.IsRecurringPayment)
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

                        // Gift card usage history.
                        if (!processPaymentRequest.IsRecurringPayment && cartTotal.AppliedGiftCards != null)
                        {
                            foreach (var agc in cartTotal.AppliedGiftCards)
                            {
                                var amountUsed = agc.AmountCanBeUsed;
                                var gcuh = new GiftCardUsageHistory
                                {
                                    GiftCard = agc.GiftCard,
                                    UsedWithOrder = order,
                                    UsedValue = amountUsed,
                                    CreatedOnUtc = utcNow
                                };
                                agc.GiftCard.GiftCardUsageHistory.Add(gcuh);
                                _giftCardService.UpdateGiftCard(agc.GiftCard);
                            }
                        }

                        // Reward points history.
                        if (cartTotal.RedeemedRewardPointsAmount > decimal.Zero)
                        {
                            customer.AddRewardPointsHistoryEntry(-cartTotal.RedeemedRewardPoints,
                                _localizationService.GetResource("RewardPoints.Message.RedeemedForOrder", order.CustomerLanguageId).FormatInvariant(order.GetOrderNumber()),
                                order,
                                cartTotal.RedeemedRewardPointsAmount);

                            _customerService.UpdateCustomer(customer);
                        }

                        // Recurring orders.
                        if (!processPaymentRequest.IsRecurringPayment && isRecurringShoppingCart)
                        {
                            // Create recurring payment (the first payment).
                            var rp = new RecurringPayment
                            {
                                CycleLength = processPaymentRequest.RecurringCycleLength,
                                CyclePeriod = processPaymentRequest.RecurringCyclePeriod,
                                TotalCycles = processPaymentRequest.RecurringTotalCycles,
                                StartDateUtc = utcNow,
                                IsActive = true,
                                CreatedOnUtc = utcNow,
                                InitialOrder = order,
                            };
                            _orderService.InsertRecurringPayment(rp);

                            var recurringPaymentType = _paymentService.GetRecurringPaymentType(processPaymentRequest.PaymentMethodSystemName);
                            switch (recurringPaymentType)
                            {
                                case RecurringPaymentType.NotSupported:
                                    break;
                                case RecurringPaymentType.Manual:
                                    {
                                        // First payment.
                                        rp.RecurringPaymentHistory.Add(new RecurringPaymentHistory
                                        {
                                            RecurringPayment = rp,
                                            CreatedOnUtc = utcNow,
                                            OrderId = order.Id
                                        });
                                        _orderService.UpdateRecurringPayment(rp);
                                    }
                                    break;
                                case RecurringPaymentType.Automatic:
                                    {
                                        // Will be created later (process is automated).
                                    }
                                    break;
                                default:
                                    break;
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

                        msg = _messageFactory.SendOrderPlacedCustomerNotification(order, order.CustomerLanguageId);
                        if (msg?.Email?.Id != null)
                        {
                            _orderService.AddOrderNote(order, T("Admin.OrderNotice.CustomerEmailQueued", msg.Email.Id));
                        }

                        // Check order status.
                        CheckOrderStatus(order);

                        // Reset checkout data.
                        if (!processPaymentRequest.IsRecurringPayment && !processPaymentRequest.IsMultiOrder)
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

                        if (!processPaymentRequest.IsRecurringPayment)
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
    }
}