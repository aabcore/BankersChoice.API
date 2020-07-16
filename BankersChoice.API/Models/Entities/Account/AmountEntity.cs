using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BankersChoice.API.Models.Entities.Account
{
    public class AmountEntity
    {
        public Decimal Amount { get; set; }

        [BsonRepresentation(BsonType.String)]
        public CurrencyEnum Currency { get; set; }
    }
}