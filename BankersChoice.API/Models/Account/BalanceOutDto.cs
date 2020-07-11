using System;
using BankersChoice.API.Models.Entities;

namespace BankersChoice.API.Models.Account
{
    public class BalanceOutDto
    {
        public AmountDto BalanceAmount { get; set; }
        public BalanceTypeEnum BalanceType { get; set; }
        public bool CreditLimitIncluded { get; set; }
        public DateTimeOffset LastChangeDateTime { get; set; }
        public string LastCommittedTransaction { get; set; }

        public static BalanceOutDto EntityToOutDto(BalanceEntity balanceEntity)
        {
            return new BalanceOutDto()
            {
                BalanceAmount = AmountDto.EntityToOutDto(balanceEntity.BalanceAmount),
                BalanceType = balanceEntity.BalanceType,
                CreditLimitIncluded = balanceEntity.CreditLimitIncluded,
                LastChangeDateTime = balanceEntity.LastChangeDateTime,
                LastCommittedTransaction = balanceEntity.LastCommittedTransaction
            };
        }
    }
}