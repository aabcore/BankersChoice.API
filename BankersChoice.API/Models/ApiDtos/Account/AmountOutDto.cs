﻿using System;
using BankersChoice.API.Models.Entities.Account;

namespace BankersChoice.API.Models.ApiDtos.Account
{
    public class AmountDto
    {
        public Decimal Amount { get; set; }
        public CurrencyEnum Currency { get; set; }

        public static AmountDto EntityToOutDto(AmountEntity amountEntity)
        {
            return new AmountDto()
            {
                Amount = amountEntity.Amount,
                Currency = amountEntity.Currency
            };
        }
    }
}