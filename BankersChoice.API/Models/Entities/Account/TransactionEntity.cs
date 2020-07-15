using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BankersChoice.API.Models.Entities.Account
{
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
}