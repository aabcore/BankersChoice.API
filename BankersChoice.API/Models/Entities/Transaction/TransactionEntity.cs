using System;
using BankersChoice.API.Models.Entities.Account;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BankersChoice.API.Models.Entities.Transaction
{
    [BsonDiscriminator(RootClass = true)]
    [BsonKnownTypes(typeof(CreditTransactionEntity), typeof(DebitTransactionEntity))]
    public class TransactionEntity
    {
        [BsonId, BsonRepresentation(BsonType.String)]
        public Guid TransactionId { get; set; }

        [BsonRepresentation(BsonType.String)]
        public Guid AssociatedAccountId { get; set; }

        [BsonRepresentation(BsonType.String)]
        public TransactionTypeEnum TransactionType { get; set; }

        public string CheckId { get; set; }
        public AmountEntity TransactionAmount { get; set; }

        [BsonRepresentation(BsonType.String)]
        public DateTimeOffset EntryDate { get; set; }

        [BsonRepresentation(BsonType.String)]
        public DateTimeOffset? BookingDate { get; set; }

        [BsonRepresentation(BsonType.String)]
        public BookingStatusEnum BookingStatus { get; set; }
    }

    public class DebitTransactionEntity : TransactionEntity
    {
        public string DebtorName { get; set; }
        public AccountReferenceEntity DebtorAccount { get; set; }
    }

    public class CreditTransactionEntity : TransactionEntity
    {
        public string CreditorName { get; set; }
        public AccountReferenceEntity CreditorAccount { get; set; }

        [BsonRepresentation(BsonType.String)]
        public DateTimeOffset? ValueDate { get; set; }
    }

    public enum TransactionTypeEnum
    {
        DEBIT,
        CREDIT
    }

    public enum BookingStatusEnum
    {
        BOOKED,
        PENDING
    }
}