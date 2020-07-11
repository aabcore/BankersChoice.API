using System;
using System.Collections.Generic;
using System.Linq;
using BankersChoice.API.Models.Entities;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson.Serialization.Attributes;

namespace BankersChoice.API.Models.Account
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
        public Lock Lock {get;set;}
        public DateTimeOffset LastModifedDate { get; set; }

        public static AccountDetailsOutDto EntityToOutDto(AccountDetailEntity accountDetailEntity)
        {
            return new AccountDetailsOutDto()
            {
                ResourceId = accountDetailEntity.ResourceId,
                Name = accountDetailEntity.Name,
                Product = accountDetailEntity.Product,
                CashAccountType = accountDetailEntity.CashAccountType,
                Status = accountDetailEntity.Status,
                Usage = accountDetailEntity.Usage,
                Balances = accountDetailEntity.Balances.Select(BalanceOutDto.EntityToOutDto),
                Lock = accountDetailEntity.Lock,
                LastModifedDate = accountDetailEntity.LastModifiedDate
            };
        }
    }
}