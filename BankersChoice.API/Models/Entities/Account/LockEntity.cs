using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BankersChoice.API.Models.Entities.Account
{
    public class LockEntity
    {
        public bool IsLocked { get; set; }

        [BsonRepresentation(BsonType.String)]
        public Guid LockedBy { get; set; }

        public string Secret { get; set; }
    }
}