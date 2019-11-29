using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReadySignOn.ReadyPay.Models
{
    public class PaymentMethod
    {
        public string network { get; set; }
        public string type { get; set; }
        public string displayName { get; set; }
    }

    public class PDHeader
    {
        public string publicKeyHash { get; set; }
        public string ephemeralPublicKey { get; set; }
        public string transactionId { get; set; }
        public string applicationData { get; set; }
    }

    public class PaymentData
    {
        public PDHeader header { get; set; }
        public string version { get; set; }
        public string signature { get; set; }
        public string data { get; set; }
    }

    public class ShippingMethod
    {
        public string detail { get; set; }
        public string identifier { get; set; }
        public int type { get; set; }
        public decimal amount { get; set; }
    }

    public class RPContact
    {
        public string emailAddress { get; set; }
        public string givenName { get; set; }
        public string middleName { get; set; }
        public string familyName { get; set; }
        public string namePrefix { get; set; }
        public string nameSuffix { get; set; }
        public string nickname { get; set; }
        public string phoneNumber { get; set; }
        public string street { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string postalCode { get; set; }
        public string country { get; set; }
        public string isoCountryCode { get; set; }
    }

    // This is the response from the payment network after the payment request submission.
    public class ReadyPayment
    {
        public string transactionIdentifier { get; set; }
        public PaymentMethod paymentMethod { get; set; }
        public PaymentData paymentData { get; set; }
        public ShippingMethod shippingMethod { get; set; }
        public RPContact billingContact { get; set; }
        public RPContact shippingContact { get; set; }
        public decimal grandTotalCharged { get; set; }
    }
}

//{
//  "transactionIdentifier": "E3BD8F35779049E6105F6DFF796764437D9591B2EFDF8C4024015F6FFDF60B21",
//  "paymentMethod": {
//    "network": "Visa",
//    "type": "credit",
//    "displayName": "Visa 2826"
//  },
//  "paymentData": {
//    "header": {
//      "publicKeyHash": "e3KIjjBTAdqh5lEd9ZLwQNmmyX4nXq8lQdQ0Dylbevw=",
//      "ephemeralPublicKey": "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEklLOhfM/mJYDgw98Uw8LBQUnGZeWJ98yjQtolKLIN65ZXvRGdL9dv4i12oXGLi0GbNgzLJLXdYHgw8MY2vgXtQ==",
//      "transactionId": "e3bd8f35779049e6105f6dff796764437d9591b2efdf8c4024015f6ffdf60b21"
//    },
//    "version": "EC_v1",
//    "signature": "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",
//    "data": "VPg3xq3y1yr7o+VHuJpARAgbKJBD0WjOx4/gwDKPrrTQcCgfCfPW8U0SMc1PdKdSYU0DsppChqhXu0/rMXY4B+DeSWIFmul3ZJgMUGexvOSQ4pmaX97mIbkATKz8xuX9RGYb0Uw3O/5+jDm8vzTSIe/MCS5pLbSwem6onmALMJUWctYMjfOpAjGE0/Duvbfbe+dK2wXKy2YRbei4H1g9Q/yUVgbirfKfgrjxvV8Q9OSioNnePpMLExwnpgNoCIKPqOz3uuWrSRx09xTi+Fw1AlrhmFlHlY//iK5igoF9TqemOi0/rxqOX+piQw6StEiRgqGB9XMlG6LPcZVayYS8FpSA3dZgGhv/k7cibfTku/N3xS5RMYS7WgR8FeMs/px9W1ttHwT00ItIN5BmnY85oOlx0vJM75SX9d6TS89hFA=="
//  }
