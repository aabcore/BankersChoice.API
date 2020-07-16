using System;
using System.Collections.Generic;
using System.Linq;
using BankersChoice.API.Models.Entities.Account;
using MongoDB.Bson.Serialization.Attributes;

namespace BankersChoice.API.Models.ApiDtos.Account
{
    public class AccountDetailsOutDto : AccountReferenceOutDto
    {
        [BsonId]
        public Guid ResourceId { get; set; }

        public string Name { get; set; }
        public string Product { get; set; }
        public ExternalCashAccountType1Code CashAccountType { get; set; }
        public AccountStatusEnum Status { get; set; }
        public UsageEnum Usage { get; set; }
        public IEnumerable<BalanceOutDto> Balances { get; set; }
        public LockOutDto Lock { get; set; }
        public DateTimeOffset LastModifiedDate { get; set; }
        public AmountDto AuthorizedLimit { get; set; }

        public static AccountDetailsOutDto EntityToOutDto(AccountDetailEntity accountDetailEntity)
        {
            var accountDetailsFromReference =
                AccountReferenceOutDto.EntityToOutDto<AccountDetailsOutDto>(accountDetailEntity);

            accountDetailsFromReference.ResourceId = accountDetailEntity.ResourceId;
            accountDetailsFromReference.Name = accountDetailEntity.Name;
            accountDetailsFromReference.Product = accountDetailEntity.Product;
            accountDetailsFromReference.CashAccountType = accountDetailEntity.CashAccountType;
            accountDetailsFromReference.Status = accountDetailEntity.Status;
            accountDetailsFromReference.Usage = accountDetailEntity.Usage;
            accountDetailsFromReference.Balances = accountDetailEntity.Balances.Select(BalanceOutDto.EntityToOutDto);
            accountDetailsFromReference.Lock = LockOutDto.EntityToOutDto(accountDetailEntity.Lock);
            accountDetailsFromReference.LastModifiedDate = accountDetailEntity.LastModifiedDate;
            accountDetailsFromReference.AuthorizedLimit = accountDetailEntity.AuthorizedLimit != null
                ? AmountDto.EntityToOutDto(accountDetailEntity.AuthorizedLimit)
                : null;
            return accountDetailsFromReference;
        }
    }
}