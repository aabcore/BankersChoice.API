using System;
using System.Collections.Generic;
using BankersChoice.API.Models.Account;
using Microsoft.Extensions.Primitives;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BankersChoice.API.Models.Entities
{
    public class UserEntity
    {
        [BsonId, BsonRepresentation(BsonType.String)]
        public Guid UserId { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
    }

    public class TransactionEntity
    {
        [BsonId, BsonRepresentation(BsonType.String)]
        public Guid TransactionId { get; set; }

        public string CheckId { get; set; }
        public string CreditorName { get; set; }
        public AccountReferenceEntity CreditorAccount { get; set; }
        public string DebtorName { get; set; }
        public AccountReferenceEntity DebtorAccount { get; set; }
        public AmountEntity TransactionAmount { get; set; }

        [BsonRepresentation(BsonType.String)]
        public DateTimeOffset BookingDate { get; set; }

        [BsonRepresentation(BsonType.String)]
        public DateTimeOffset? ValueDate { get; set; }
    }

    public class AccountReferenceEntity
    {
        // Primary Account Number
        public string Pan { get; set; }
        public RoutingNumbersEntity RoutingNumbers { get; set; }

        [BsonRepresentation(BsonType.String)]
        public CurrencyEnum Currency { get; set; }

        // Mobile Phone Number (but why?)
        public string Msisdn { get; set; }

        public class RoutingNumbersEntity
        {
            public string Ach { get; set; }
            public string Wire { get; set; }
        }
    }

    public class AccountDetailEntity : AccountReferenceEntity
    {
        [BsonId, BsonRepresentation(BsonType.String)]
        public Guid ResourceId { get; set; }

        public string Name { get; set; }
        public string Product { get; set; }

        [BsonRepresentation(BsonType.String)]
        public ExternalCashAccountType1Code CashAccountType { get; set; }

        [BsonRepresentation(BsonType.String)]
        public AccountStatusEnum Status { get; set; }

        [BsonRepresentation(BsonType.String)]
        public UsageEnum Usage { get; set; }

        public IEnumerable<BalanceEntity> Balances { get; set; }
        public Lock Lock { get; set; }

        [BsonRepresentation(BsonType.String)]
        public DateTimeOffset LastModifiedDate { get; set; }
    }

    public class BalanceEntity
    {
        public AmountEntity BalanceAmount { get; set; }

        [BsonRepresentation(BsonType.String)]
        public BalanceTypeEnum BalanceType { get; set; }

        public bool CreditLimitIncluded { get; set; }

        [BsonRepresentation(BsonType.String)]
        public DateTimeOffset LastChangeDateTime { get; set; }

        public string LastCommittedTransaction { get; set; }
    }

    public class AmountEntity
    {
        public Decimal Amount { get; set; }
        public CurrencyEnum Currency { get; set; }
    }
}