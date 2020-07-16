using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BankersChoice.API.Models.Entities.Account
{
    public class BalanceEntity
    {
        public AmountEntity BalanceAmount { get; set; }

        [BsonRepresentation(BsonType.String)]
        public BalanceTypeEnum BalanceType { get; set; }

        public bool CreditLimitIncluded { get; set; }

        [BsonRepresentation(BsonType.String)]
        public DateTimeOffset LastChangeDateTime { get; set; }

        [BsonRepresentation(BsonType.String)]
        public Guid? LastCommittedTransaction { get; set; }
    }
}