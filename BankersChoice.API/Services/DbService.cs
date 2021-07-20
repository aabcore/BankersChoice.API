using System;
using BankersChoice.API.Models;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;

namespace BankersChoice.API.Services
{
    public abstract class DbService
    {
        private IConfiguration Config { get; }

        public DbService(IConfiguration config)
        {
            Config = config;
        }
        public IMongoDatabase BuildDatabaseClient(DatabaseSettings dbSettings)
        {
            MongoClient client;
            if (string.IsNullOrWhiteSpace(Config["MONGO_URL"]))
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
                client = new MongoClient(clientSettings);

            }
            else
            {
                client = new MongoClient(Config["MONGO_URL"]);
            }

            return client.GetDatabase(dbSettings.DatabaseName);
        }

        public FindOneAndUpdateOptions<T> GetEntityAfterUpdateOption<T>() => new FindOneAndUpdateOptions<T>()
        {
            ReturnDocument = ReturnDocument.After
        };
    }
}