using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using BankersChoice.API.Controllers;
using BankersChoice.API.Models;
using BankersChoice.API.Models.ApiDtos.Account;
using BankersChoice.API.Models.Entities.Account;
using BankersChoice.API.Randomization;
using BankersChoice.API.Results;
using Microsoft.Extensions.Logging.EventLog;
using MongoDB.Driver;

namespace BankersChoice.API.Services
{
    public class AccountService : DbService
    {
        private readonly IMongoCollection<AccountDetailEntity> _accounts;

        public AccountService(DatabaseSettings dbSettings)
        {
            var database = BuildDatabaseClient(dbSettings);
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

            return accountDetailEntity != null
                ? AccountDetailsOutDto.EntityToOutDto(accountDetailEntity)
                : null;
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
                Pan = string.Join("", "000123456789".Select(c => rand.Next(0, 10))),
                Msisdn = accountIn.Msisdn,
                RoutingNumbers = new AccountReferenceEntity.RoutingNumbersEntity()
                {
                    Ach = AabRoutingNumbers.GetRandomRoutingNumber()
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

        public async Task<LockableResult<AccountDetailsOutDto>> Update(Guid resourceId,
            AccountUpdateDto accountUpdateDto)
        {
            var foundAccountResult = await GetEntity(resourceId);

            AccountDetailEntity foundAccount;
            switch (foundAccountResult)
            {
                case FailedTypedResult<AccountDetailEntity> failedTypedResult:
                    return new FailedLockableResult<AccountDetailsOutDto>(failedTypedResult.Error);
                case NotFoundTypedResult<AccountDetailEntity> _:
                    return new NotFoundLockableResult<AccountDetailsOutDto>();
                case SuccessfulTypedResult<AccountDetailEntity> successfulTypedResult:
                    foundAccount = successfulTypedResult.Value;
                    break;
                default:
                    return new FailedLockableResult<AccountDetailsOutDto>(
                        new ArgumentOutOfRangeException(nameof(foundAccountResult)));
            }

            if (foundAccount.Lock == null)
            {
                return new NotLockedLockableResult<AccountDetailsOutDto>();
            }

            if (foundAccount.Lock.Secret != accountUpdateDto.LockSecret)
            {
                return new LockedLockableResult<AccountDetailsOutDto>();
            }


            var updates = new List<UpdateDefinition<AccountDetailEntity>>();

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
                return new SuccessfulLockableResult<AccountDetailsOutDto>(await this.Get(resourceId));
            }

            // Set Last Modified Date
            updates.Add(Builders<AccountDetailEntity>.Update.Set(ade => ade.LastModifiedDate, DateTimeOffset.UtcNow));

            var opts = new FindOneAndUpdateOptions<AccountDetailEntity>()
            {
                ReturnDocument = ReturnDocument.After
            };

            var updatedAccount = await _accounts.FindOneAndUpdateAsync<AccountDetailEntity>(
                f => f.ResourceId == resourceId && f.Lock != null && f.Lock.Secret == accountUpdateDto.LockSecret,
                Builders<AccountDetailEntity>.Update.Combine(updates), opts);

            if (updatedAccount != null)
            {
                return new SuccessfulLockableResult<AccountDetailsOutDto>(
                    AccountDetailsOutDto.EntityToOutDto(updatedAccount));
            }
            else
            {
                return new FailedLockableResult<AccountDetailsOutDto>(
                    new Exception("Lock status changed unexpectedly. No update."));
            }
        }

        public async Task<LockableResult<LockAccountOutDto>> Lock(Guid resourceId, Guid userId)
        {
            var existingAccount = await this.Get(resourceId);
            if (existingAccount == null)
            {
                return new NotFoundLockableResult<LockAccountOutDto>();
            }

            var generatedSecret = Guid.NewGuid().ToString().Replace("-", "");

            var opts = new FindOneAndUpdateOptions<AccountDetailEntity>()
            {
                ReturnDocument = ReturnDocument.After
            };

            var updatedAccount = await _accounts.FindOneAndUpdateAsync<AccountDetailEntity>(
                f => f.ResourceId == resourceId && f.Lock == null, Builders<AccountDetailEntity>.Update.Set(
                    ade => ade.Lock, new LockEntity()
                    {
                        IsLocked = true,
                        LockedBy = userId,
                        Secret = generatedSecret
                    }), opts);


            if (updatedAccount == null)
            {
                return new SuccessfulLockableResult<LockAccountOutDto>(new LockAccountOutDto()
                {
                    GotLock = false,
                    LockSecret = null,
                    ResourceId = existingAccount.ResourceId,
                    UserId = userId
                });
            }

            else
            {
                return new SuccessfulLockableResult<LockAccountOutDto>(new LockAccountOutDto()
                {
                    GotLock = true,
                    LockSecret = generatedSecret,
                    ResourceId = existingAccount.ResourceId,
                    UserId = userId
                });
            }
        }

        public async Task<TypedResult<UnlockAccountOutDto>> Unlock(Guid resourceId, string lockSecret)
        {
            var existingAccountResult = await GetEntity(resourceId);

            AccountDetailEntity existingAccount;
            switch (existingAccountResult)
            {
                case FailedTypedResult<AccountDetailEntity> failedTypedResult:
                    return new FailedTypedResult<UnlockAccountOutDto>(failedTypedResult.Error);
                case NotFoundTypedResult<AccountDetailEntity> notFoundTypedResult:
                    return new NotFoundTypedResult<UnlockAccountOutDto>();
                case SuccessfulTypedResult<AccountDetailEntity> successfulTypedResult:
                    existingAccount = successfulTypedResult.Value;
                    break;
                default:
                    return new FailedTypedResult<UnlockAccountOutDto>(
                        new ArgumentOutOfRangeException(nameof(existingAccountResult)));
            }

            if (existingAccount.Lock == null)
            {
                return new SuccessfulTypedResult<UnlockAccountOutDto>(new UnlockAccountOutDto()
                    {SuccessfullyUnlocked = false});
            }

            if (existingAccount.Lock.Secret != lockSecret)
            {
                return new SuccessfulTypedResult<UnlockAccountOutDto>(new UnlockAccountOutDto()
                    {SuccessfullyUnlocked = false});
            }

            var opts = new FindOneAndUpdateOptions<AccountDetailEntity>()
            {
                ReturnDocument = ReturnDocument.After
            };
            var updatedAccount = await _accounts.FindOneAndUpdateAsync<AccountDetailEntity>(
                f => f.ResourceId == resourceId && f.Lock != null && f.Lock.Secret == lockSecret,
                Builders<AccountDetailEntity>.Update.Unset(ade => ade.Lock), opts);

            if (updatedAccount == null)
            {
                return new SuccessfulTypedResult<UnlockAccountOutDto>(new UnlockAccountOutDto()
                    {SuccessfullyUnlocked = false});
            }
            else
            {
                return new SuccessfulTypedResult<UnlockAccountOutDto>(new UnlockAccountOutDto()
                {
                    SuccessfullyUnlocked = true
                });
            }
        }

        public async Task<TypedResult<UnlockAccountOutDto>> ForceUnlock(Guid resourceId)
        {
            var foundAccountResult = await this.GetEntity(resourceId);

            AccountDetailEntity foundAccount;
            switch (foundAccountResult)
            {
                case FailedTypedResult<AccountDetailEntity> failedTypedResult:
                    return new FailedTypedResult<UnlockAccountOutDto>(failedTypedResult.Error);
                case NotFoundTypedResult<AccountDetailEntity> notFoundTypedResult:
                    return new NotFoundTypedResult<UnlockAccountOutDto>();
                case SuccessfulTypedResult<AccountDetailEntity> successfulTypedResult:
                    foundAccount = successfulTypedResult.Value;
                    break;
                default:
                    return new FailedTypedResult<UnlockAccountOutDto>(
                        new ArgumentOutOfRangeException(nameof(foundAccountResult)));
            }

            var opts = new FindOneAndUpdateOptions<AccountDetailEntity>()
            {
                ReturnDocument = ReturnDocument.After
            };
            var updatedAccount = await _accounts.FindOneAndUpdateAsync<AccountDetailEntity>(
                f => f.ResourceId == resourceId,
                Builders<AccountDetailEntity>.Update.Unset(ade => ade.Lock), opts);
            if (updatedAccount.Lock == null)
            {
                return new SuccessfulTypedResult<UnlockAccountOutDto>(new UnlockAccountOutDto()
                    {SuccessfullyUnlocked = true});
            }
            else
            {
                return new SuccessfulTypedResult<UnlockAccountOutDto>(new UnlockAccountOutDto()
                    {SuccessfullyUnlocked = false});
            }
        }

        private async Task<TypedResult<AccountDetailEntity>> GetEntity(Guid resourceId)
        {
            try
            {
                var foundAccount = (await _accounts.FindAsync(a => a.ResourceId == resourceId))
                    .FirstOrDefault();

                if (foundAccount == null)
                {
                    return new NotFoundTypedResult<AccountDetailEntity>();
                }
                else
                {
                    return new SuccessfulTypedResult<AccountDetailEntity>(foundAccount);
                }
            }
            catch (Exception e)
            {
                return new FailedTypedResult<AccountDetailEntity>(e);
            }
        }
    }
}