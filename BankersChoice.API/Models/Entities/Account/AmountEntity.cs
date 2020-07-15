using System;

namespace BankersChoice.API.Models.Entities.Account
{
    public class AmountEntity
    {
        public Decimal Amount { get; set; }
        public CurrencyEnum Currency { get; set; }
    }
}