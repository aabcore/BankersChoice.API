using System;
using BankersChoice.API.Models;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;

namespace BankersChoice.API.Services
{
    public abstract class DbService
    {
        public IMongoDatabase BuildDatabaseClient(DatabaseSettings dbSettings)
        {
            Console.WriteLine($"Configured with these Settings: {Environment.NewLine}" +
                              $"{JToken.FromObject(dbSettings).ToString()}");
            var credential = MongoCredential.CreateCredential("admin",
                dbSettings.DatabaseUserName,
                dbSettings.DatabasePassword);
            var clientSettings = new MongoClientSettings()
            {
                Credential = credential,
                Server = MongoServerAddress.Parse(dbSettings.ConnectionString),
                AllowInsecureTls = true
            };
            var client = new MongoClient(clientSettings);

            return client.GetDatabase(dbSettings.DatabaseName);
        }

        public FindOneAndUpdateOptions<T> GetEntityAfterUpdateOption<T>() => new FindOneAndUpdateOptions<T>()
        {
            ReturnDocument = ReturnDocument.After
        };
    }
}