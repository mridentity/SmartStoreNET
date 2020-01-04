using SmartStore.Core.Domain.Common;
using SmartStore.Services.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReadySignOn.ReadyPay.Services
{
    internal static class ReadyPayExtensions
    {
        internal static Address FindAddress(this List<Address> addresses, Address address, bool uncompleteToo)
        {
            var match = addresses.FindAddress(address.FirstName, address.LastName,
                address.PhoneNumber, address.Email, address.FaxNumber, address.Company,
                address.Address1, address.Address2,
                address.City, address.StateProvinceId, address.ZipPostalCode, address.CountryId);

            if (match == null && uncompleteToo)
            {
                // Compare with ToAddress
                match = addresses.FirstOrDefault(x =>
                    x.FirstName == null && x.LastName == null &&
                    x.Address1 == null && x.Address2 == null &&
                    x.City == address.City && x.ZipPostalCode == address.ZipPostalCode &&
                    x.PhoneNumber == null &&
                    x.CountryId == address.CountryId && x.StateProvinceId == address.StateProvinceId
                );
            }

            return match;
        }
    }
}