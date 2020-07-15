using BankersChoice.API.Models;
using BankersChoice.API.Models.Entities;
using MongoDB.Driver;

namespace BankersChoice.API.Services
{
    public class UserService : DbService
    {
        private IMongoCollection<UserEntity> _users;

        public UserService(DatabaseSettings dbSettings)
        {
            var database = BuildDatabaseClient(dbSettings);
            _users = database.GetCollection<UserEntity>(dbSettings.UsersCollectionName);
        }
    }
}