using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BankersChoice.API.Models.Entities.Account
{
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
        public LockEntity Lock { get; set; }

        [BsonRepresentation(BsonType.String)]
        public DateTimeOffset LastModifiedDate { get; set; }

        [BsonRepresentation(BsonType.String)]
        public DateTimeOffset CreatedDate { get; set; }

        public AmountEntity AuthorizedLimit { get; set; }
    }
}