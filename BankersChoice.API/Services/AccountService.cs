using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using BankersChoice.API.Controllers;
using BankersChoice.API.Models;
using BankersChoice.API.Models.Account;
using BankersChoice.API.Models.Entities;
using MongoDB.Driver;

namespace BankersChoice.API.Services
{
    public class AccountService
    {
        private readonly IMongoCollection<AccountDetailEntity> _accounts;

        public AccountService(DatabaseSettings dbSettings)
        {
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

            var database = client.GetDatabase(dbSettings.DatabaseName);


            _accounts = database.GetCollection<AccountDetailEntity>(dbSettings.AccountsCollectionName);
        }

        public class GetFilter
        {
            public AccountStatusEnum? status { get; set; }
            public ExternalCashAccountType1Code? cashAccountType { get; set; }
            public UsageEnum? usage { get; set; }
            public DateTimeOffset? lastModifiedBefore { get; set; }
            public DateTimeOffset? lastModifiedAfter { get; set; }
        }

        public async Task<IEnumerable<AccountDetailsOutDto>> Get(GetFilter getFilter)
        {
            List<AccountDetailEntity> accountDetailEntities;
            var searchFilters = new List<FilterDefinition<AccountDetailEntity>>();
            if (getFilter != null)
            {
                if (getFilter.status != null)
                    searchFilters.Add(Builders<AccountDetailEntity>.Filter.Eq(ade => ade.Status, getFilter.status));
                if (getFilter.cashAccountType != null)
                    searchFilters.Add(Builders<AccountDetailEntity>.Filter.Eq(ade => ade.CashAccountType,
                        getFilter.cashAccountType));
                if (getFilter.usage != null)
                    searchFilters.Add(Builders<AccountDetailEntity>.Filter.Eq(ade => ade.Usage, getFilter.usage));
                if (getFilter.lastModifiedAfter != null)
                    searchFilters.Add(Builders<AccountDetailEntity>.Filter.Gt(ade => ade.LastModifiedDate,
                        getFilter.lastModifiedAfter));
                if (getFilter.lastModifiedBefore != null)
                    searchFilters.Add(Builders<AccountDetailEntity>.Filter.Lt(ade => ade.LastModifiedDate,
                        getFilter.lastModifiedBefore));
            }

            if (!searchFilters.Any())
            {
                searchFilters.Add(Builders<AccountDetailEntity>.Filter.Empty);
            }

            accountDetailEntities =
                (await _accounts.FindAsync(Builders<AccountDetailEntity>.Filter.And(searchFilters))).ToList();


            return accountDetailEntities.Select(AccountDetailsOutDto.EntityToOutDto);
        }

        public async Task<AccountDetailsOutDto> Get(Guid id)
        {
            var accountDetailEntity = (await _accounts.FindAsync(a => a.ResourceId == id))
                .FirstOrDefault();

            if (accountDetailEntity != null)
            {
                return AccountDetailsOutDto.EntityToOutDto(accountDetailEntity);
            }
            else
            {
                return null;
            }
        }

        public async Task<AccountDetailsOutDto> Create(AccountNewDto accountIn)
        {
            var rand = new Random();
            var accountDetailEntity = new AccountDetailEntity()
            {
                ResourceId = Guid.NewGuid(),
                Name = accountIn.Name,
                Product = accountIn.Product,
                CashAccountType = accountIn.CashAccountType,
                Status = accountIn.Status,
                Usage = accountIn.Usage,
                Lock = null,
                Pan = rand.Next(10000000, 99999999).ToString(),
                Msisdn = accountIn.Msisdn,
                RoutingNumbers = new AccountReferenceEntity.RoutingNumbersEntity()
                {
                    Ach = rand.Next(1000000, 9999999).ToString()
                },
                Balances = new BalanceEntity[]
                {
                    new BalanceEntity()
                    {
                        BalanceType = BalanceTypeEnum.expected,
                        BalanceAmount = new AmountEntity()
                        {
                            Amount = accountIn.InitialBalance.Amount,
                            Currency = accountIn.InitialBalance.Currency
                        },
                        CreditLimitIncluded = false,
                        LastChangeDateTime = DateTimeOffset.UtcNow,
                        LastCommittedTransaction = string.Empty
                    },
                }
            };

            if (accountIn.CashAccountType == ExternalCashAccountType1Code.CHAR)
            {
                accountDetailEntity.Balances = accountDetailEntity.Balances.Concat(new BalanceEntity[]
                {
                    new BalanceEntity()
                    {
                        BalanceType = BalanceTypeEnum.authorised,
                        BalanceAmount = new AmountEntity()
                        {
                            Amount = accountIn.InitialBalance.Amount + 1000,
                            Currency = accountIn.InitialBalance.Currency
                        },
                        CreditLimitIncluded = true,
                        LastChangeDateTime = DateTimeOffset.UtcNow,
                        LastCommittedTransaction = string.Empty
                    },
                });
            }

            accountDetailEntity.LastModifiedDate = DateTimeOffset.UtcNow;

            await _accounts.InsertOneAsync(accountDetailEntity);
            return AccountDetailsOutDto.EntityToOutDto(accountDetailEntity);
        }

        public async Task<AccountDetailsOutDto> Update(Guid accountReferenceId, AccountUpdateDto accountUpdateDto)
        {
            List<UpdateDefinition<AccountDetailEntity>> updates = new List<UpdateDefinition<AccountDetailEntity>>();

            if (!string.IsNullOrWhiteSpace(accountUpdateDto.Name))
            {
                updates.Add(Builders<AccountDetailEntity>.Update.Set(ade => ade.Name, accountUpdateDto.Name));
            }

            if (!string.IsNullOrWhiteSpace(accountUpdateDto.Msisdn))
            {
                updates.Add(Builders<AccountDetailEntity>.Update.Set(ade => ade.Msisdn, accountUpdateDto.Msisdn));
            }

            if (accountUpdateDto.Status != null)
            {
                updates.Add(Builders<AccountDetailEntity>.Update.Set(ade => ade.Status, accountUpdateDto.Status));
            }

            if (!updates.Any())
            {
                return await this.Get(accountReferenceId);
            }

            // Set Last Modified Date
            updates.Add(Builders<AccountDetailEntity>.Update.Set(ade => ade.LastModifiedDate, DateTimeOffset.UtcNow));

            var opts = new FindOneAndUpdateOptions<AccountDetailEntity>()
            {
                ReturnDocument = ReturnDocument.After
            };

            var foundAccount = await _accounts.FindOneAndUpdateAsync<AccountDetailEntity>(
                f => f.ResourceId == accountReferenceId,
                Builders<AccountDetailEntity>.Update.Combine(updates), opts);

            return foundAccount != null ? AccountDetailsOutDto.EntityToOutDto(foundAccount) : null;
        }
    }
}