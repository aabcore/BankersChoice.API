using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BankersChoice.API.Models.Entities.Account
{
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
}