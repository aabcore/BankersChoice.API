using System;
using System.Linq;
using BankersChoice.API.Models.Entities;
using MongoDB.Bson.Serialization.Attributes;

namespace BankersChoice.API.Models.Account
{
    public class AccountReferenceOutDto
    {
        public string Pan { get; set; }
        public string MaskedPan { get; set; }
        public RoutingNumbersOutDto RoutingNumbers { get; set; }
        public CurrencyEnum Currency { get; set; }
        public string Msisdn { get; set; }

        public static AccountReferenceOutDto EntityToOutDto(AccountReferenceEntity accountReferenceEntity)
        {
            return new AccountReferenceOutDto()
            {
                Pan = accountReferenceEntity.Pan,
                MaskedPan = accountReferenceEntity.Pan.Substring(0, 2) +
                            Enumerable.Repeat("x", accountReferenceEntity.Pan.Length - 4) + 
                            accountReferenceEntity.Pan.Substring(accountReferenceEntity.Pan.Length - 2, 2),
                Msisdn = accountReferenceEntity.Msisdn,
                Currency = accountReferenceEntity.Currency,
                RoutingNumbers = RoutingNumbersOutDto.EntityToOutDto(accountReferenceEntity.RoutingNumbers)
            };
        }
    }
}