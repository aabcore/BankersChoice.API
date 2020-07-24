using System;
using BankersChoice.API.Models.Entities.Account;

namespace BankersChoice.API.Models.ApiDtos.Account
{
    public class BalanceOutDto
    {
        public AmountOutDto BalanceAmount { get; set; }
        public BalanceTypeEnum BalanceType { get; set; }
        public bool CreditLimitIncluded { get; set; }
        public DateTimeOffset LastChangeDateTime { get; set; }
        public Guid? LastCommittedTransaction { get; set; }

        public static BalanceOutDto EntityToOutDto(BalanceEntity balanceEntity)
        {
            return new BalanceOutDto()
            {
                BalanceAmount = AmountOutDto.EntityToOutDto(balanceEntity.BalanceAmount),
                BalanceType = balanceEntity.BalanceType,
                CreditLimitIncluded = balanceEntity.CreditLimitIncluded,
                LastChangeDateTime = balanceEntity.LastChangeDateTime,
                LastCommittedTransaction = balanceEntity.LastCommittedTransaction
            };
        }
    }
}