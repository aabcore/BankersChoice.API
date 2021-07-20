using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using BankersChoice.API.Controllers;
using BankersChoice.API.Models;
using BankersChoice.API.Models.ApiDtos;
using BankersChoice.API.Models.ApiDtos.Account;
using BankersChoice.API.Models.Entities;
using BankersChoice.API.Models.Entities.Account;
using BankersChoice.API.Models.Entities.Transaction;
using BankersChoice.API.Randomization;
using BankersChoice.API.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using MongoDB.Driver;
using Exception = System.Exception;

namespace BankersChoice.API.Services
{
    public class AccountService : DbService
    {
        private UserService UserService { get; }
        private readonly IMongoCollection<AccountDetailEntity> _accounts;

        public AccountService(DatabaseSettings dbSettings, UserService userService, IConfiguration config) : base(config)
        {
            UserService = userService;
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

            var accountDetailEntities =
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

        public async Task<LockableResult<AccountDetailsOutDto>> Create(AccountNewDto accountIn)
        {
            if (accountIn.AuthorizedLimit != null && accountIn.AuthorizedLimit.Currency != accountIn.InitialBalance.Currency)
            {
                return new BadRequestLockableResult<AccountDetailsOutDto>(BadRequestOutDto.WrongCurrencyType);
            }
            var rand = new Random();
            var dateTimeNow = DateTimeOffset.UtcNow;
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
                Currency = accountIn.InitialBalance.Currency,
                RoutingNumbers = new AccountReferenceEntity.RoutingNumbersEntity()
                {
                    Ach = AabRoutingNumbers.GetRandomRoutingNumber()
                },
                Balances = new BalanceEntity[]
                {
                    new BalanceEntity()
                    {
                        BalanceType = BalanceTypeEnum.closingBooked,
                        BalanceAmount = accountIn.InitialBalance.ToEntity(),
                        CreditLimitIncluded = false,
                        LastChangeDateTime = dateTimeNow,
                        LastCommittedTransaction = null
                    },
                },
                AuthorizedLimit = accountIn.AuthorizedLimit?.ToEntity()
            };

            if (accountIn.AuthorizedLimit != null)
            {
                accountDetailEntity.Balances = accountDetailEntity.Balances.Concat(new BalanceEntity[]
                {
                    new BalanceEntity()
                    {
                        BalanceType = BalanceTypeEnum.authorised,
                        BalanceAmount = accountIn.InitialBalance.ToEntity().Add(accountIn.AuthorizedLimit.ToEntity()),
                        CreditLimitIncluded = true,
                        LastChangeDateTime = dateTimeNow,
                        LastCommittedTransaction = null
                    },
                });
            }

            accountDetailEntity.LastModifiedDate = dateTimeNow;
            accountDetailEntity.CreatedDate = dateTimeNow;

            await _accounts.InsertOneAsync(accountDetailEntity);
            return new SuccessfulLockableResult<AccountDetailsOutDto>(AccountDetailsOutDto.EntityToOutDto(accountDetailEntity));
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

            var updatedAccount = await _accounts.FindOneAndUpdateAsync<AccountDetailEntity>(
                f => f.ResourceId == resourceId && f.Lock != null && f.Lock.Secret == accountUpdateDto.LockSecret,
                Builders<AccountDetailEntity>.Update.Combine(updates),
                GetEntityAfterUpdateOption<AccountDetailEntity>());

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

        public class LockCheck
        {
            public bool AccountLocked { get; set; }
            public bool CorrectSecret { get; set; }
        }

        public async Task<TypedResult<LockCheck>> CheckLock(Guid resourceId, string secret)
        {
            var foundAccountResult = await GetEntity(resourceId);
            AccountDetailEntity foundAccount;
            switch (foundAccountResult)
            {
                case BadRequestTypedResult<AccountDetailEntity> badRequestTypedResult:
                    return new BadRequestTypedResult<LockCheck>(badRequestTypedResult.Problem);
                case FailedTypedResult<AccountDetailEntity> failedTypedResult:
                    return new FailedTypedResult<LockCheck>(failedTypedResult.Error);
                case NotFoundTypedResult<AccountDetailEntity> _:
                    return new NotFoundTypedResult<LockCheck>();
                case SuccessfulTypedResult<AccountDetailEntity> successfulTypedResult:
                    foundAccount = successfulTypedResult.Value;
                    break;
                default:
                    return new FailedTypedResult<LockCheck>(new ArgumentOutOfRangeException(nameof(foundAccountResult)));
            }

            if (foundAccount.Lock == null)
            {
                return new SuccessfulTypedResult<LockCheck>(new LockCheck() {AccountLocked = false});
            }

            if (foundAccount.Lock.Secret == secret)
            {
                return new SuccessfulTypedResult<LockCheck>(new LockCheck() {AccountLocked = true, CorrectSecret = true});
            }
            else
            {
                return new SuccessfulTypedResult<LockCheck>(new LockCheck() {AccountLocked = true, CorrectSecret = false});
            }
        }

        public async Task<LockableResult<LockAccountOutDto>> Lock(Guid resourceId, Guid userId)
        {
            var existingAccount = await this.Get(resourceId);
            if (existingAccount == null)
            {
                return new NotFoundLockableResult<LockAccountOutDto>();
            }

            var foundUser = await UserService.Get(userId);
            if (foundUser is NotFoundTypedResult<UserEntity>)
            {
                return new BadRequestLockableResult<LockAccountOutDto>("Given userId doesn't exist.");
            }

            var generatedSecret = Guid.NewGuid().ToString().Replace("-", "");

            var updatedAccount = await _accounts.FindOneAndUpdateAsync<AccountDetailEntity>(
                f => f.ResourceId == resourceId && f.Lock == null, Builders<AccountDetailEntity>.Update.Set(
                    ade => ade.Lock, new LockEntity()
                    {
                        IsLocked = true,
                        LockedBy = userId,
                        Secret = generatedSecret
                    }), GetEntityAfterUpdateOption<AccountDetailEntity>());


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

            var updatedAccount = await _accounts.FindOneAndUpdateAsync<AccountDetailEntity>(
                f => f.ResourceId == resourceId && f.Lock != null && f.Lock.Secret == lockSecret,
                Builders<AccountDetailEntity>.Update.Unset(ade => ade.Lock),
                GetEntityAfterUpdateOption<AccountDetailEntity>());

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

            var updatedAccount = await _accounts.FindOneAndUpdateAsync<AccountDetailEntity>(
                f => f.ResourceId == resourceId,
                Builders<AccountDetailEntity>.Update.Unset(ade => ade.Lock),
                GetEntityAfterUpdateOption<AccountDetailEntity>());
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

        public async Task<TypedResult<AccountDetailEntity>> GetEntity(Guid resourceId)
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

        public async Task<Result> UpdateBalance_NewDebit(DebitTransactionEntity newDebitTransaction)
        {
            try
            {
                var dateTimeNow = DateTimeOffset.UtcNow;
                var foundAccountResult = await GetEntity(newDebitTransaction.AssociatedAccountId);
                AccountDetailEntity foundAccount;
                switch (foundAccountResult)
                {
                    case BadRequestTypedResult<AccountDetailEntity> badRequestTypedResult:
                        return new FailedResult(new Exception(badRequestTypedResult.Problem.Message));
                    case FailedTypedResult<AccountDetailEntity> failedTypedResult:
                        return new FailedResult(failedTypedResult.Error);
                    case NotFoundTypedResult<AccountDetailEntity> notFoundTypedResult:
                        return new FailedResult(new Exception("Account Not Found"));
                    case SuccessfulTypedResult<AccountDetailEntity> successfulTypedResult:
                        foundAccount = successfulTypedResult.Value;
                        break;
                    default:
                        return new FailedResult(new ArgumentOutOfRangeException(nameof(foundAccountResult)));
                }

                var balances = foundAccount.Balances.ToList();
                var closingBookedBalance = balances.First(b => b.BalanceType == BalanceTypeEnum.closingBooked);
                var expectedBalance = balances.FirstOrDefault(b => b.BalanceType == BalanceTypeEnum.expected);
                if (expectedBalance == null)
                {
                    expectedBalance = new BalanceEntity()
                    {
                        BalanceType = BalanceTypeEnum.expected,
                        BalanceAmount = closingBookedBalance.BalanceAmount.RealCopy(),
                        CreditLimitIncluded = false,
                        LastChangeDateTime = dateTimeNow
                    };
                    balances.Add(expectedBalance);
                }

                expectedBalance.BalanceAmount = expectedBalance.BalanceAmount.Subtract(newDebitTransaction.TransactionAmount);
                expectedBalance.LastChangeDateTime = dateTimeNow;
                expectedBalance.LastCommittedTransaction = newDebitTransaction.TransactionId;

                if (foundAccount.AuthorizedLimit != null)
                {
                    var authorizedBalance = balances.FirstOrDefault(b => b.BalanceType == BalanceTypeEnum.authorised);
                    if (authorizedBalance == null)
                    {
                        authorizedBalance = new BalanceEntity()
                        {
                            BalanceType = BalanceTypeEnum.authorised,
                            BalanceAmount = expectedBalance.BalanceAmount.RealCopy(),
                            CreditLimitIncluded = true,
                            LastChangeDateTime = dateTimeNow
                        };
                        balances.Add(authorizedBalance);
                    }

                    authorizedBalance.BalanceAmount = expectedBalance.BalanceAmount.Add(foundAccount.AuthorizedLimit);
                    authorizedBalance.LastChangeDateTime = dateTimeNow;
                    authorizedBalance.LastCommittedTransaction = newDebitTransaction.TransactionId;
                }

                foundAccount.Balances = balances;
                foundAccount.LastModifiedDate = dateTimeNow;

                _accounts.FindOneAndReplace(a => a.ResourceId == foundAccount.ResourceId, foundAccount);
                return new SuccessResult();
            }
            catch (Exception e)
            {
                return new FailedResult(e);
            }
        }

        public async Task<Result> UpdateBalance_NewCredit(CreditTransactionEntity newCreditTransaction)
        {
            try
            {
                var dateTimeNow = DateTimeOffset.UtcNow;
                var foundAccountResult = await GetEntity(newCreditTransaction.AssociatedAccountId);
                AccountDetailEntity foundAccount;
                switch (foundAccountResult)
                {
                    case BadRequestTypedResult<AccountDetailEntity> badRequestTypedResult:
                        return new FailedResult(new Exception(badRequestTypedResult.Problem.Message));
                    case FailedTypedResult<AccountDetailEntity> failedTypedResult:
                        return new FailedResult(failedTypedResult.Error);
                    case NotFoundTypedResult<AccountDetailEntity> notFoundTypedResult:
                        return new FailedResult(new Exception("Account Not Found"));
                    case SuccessfulTypedResult<AccountDetailEntity> successfulTypedResult:
                        foundAccount = successfulTypedResult.Value;
                        break;
                    default:
                        return new FailedResult(new ArgumentOutOfRangeException(nameof(foundAccountResult)));
                }

                var balances = foundAccount.Balances.ToList();
                var closingBookedBalance = balances.First(b => b.BalanceType == BalanceTypeEnum.closingBooked);
                var expectedBalance = balances.FirstOrDefault(b => b.BalanceType == BalanceTypeEnum.expected);
                if (expectedBalance == null)
                {
                    expectedBalance = new BalanceEntity()
                    {
                        BalanceType = BalanceTypeEnum.expected,
                        BalanceAmount = closingBookedBalance.BalanceAmount.RealCopy(),
                        CreditLimitIncluded = false,
                    };
                    balances.Add(expectedBalance);
                }

                expectedBalance.BalanceAmount = expectedBalance.BalanceAmount.Add(newCreditTransaction.TransactionAmount);
                expectedBalance.LastChangeDateTime = dateTimeNow;
                expectedBalance.LastCommittedTransaction = newCreditTransaction.TransactionId;

                if (foundAccount.AuthorizedLimit != null)
                {
                    var authorizedBalance = balances.FirstOrDefault(b => b.BalanceType == BalanceTypeEnum.authorised);
                    if (authorizedBalance == null)
                    {
                        authorizedBalance = new BalanceEntity()
                        {
                            BalanceType = BalanceTypeEnum.authorised,
                            CreditLimitIncluded = true,
                        };
                        balances.Add(authorizedBalance);
                    }

                    authorizedBalance.BalanceAmount = expectedBalance.BalanceAmount.Add(foundAccount.AuthorizedLimit);
                    authorizedBalance.LastChangeDateTime = dateTimeNow;
                    authorizedBalance.LastCommittedTransaction = newCreditTransaction.TransactionId;
                }

                foundAccount.Balances = balances;
                foundAccount.LastModifiedDate = dateTimeNow;

                _accounts.FindOneAndReplace(a => a.ResourceId == foundAccount.ResourceId, foundAccount);
                return new SuccessResult();
            }
            catch (Exception e)
            {
                return new FailedResult(e);
            }
        }

        public async Task<Result> UpdateBalance_BookCredit(CreditTransactionEntity newCreditTransaction)
        {
            try
            {
                var dateTimeNow = DateTimeOffset.UtcNow;
                var foundAccountResult = await GetEntity(newCreditTransaction.AssociatedAccountId);
                AccountDetailEntity foundAccount;
                switch (foundAccountResult)
                {
                    case BadRequestTypedResult<AccountDetailEntity> badRequestTypedResult:
                        return new FailedResult(new Exception(badRequestTypedResult.Problem.Message));
                    case FailedTypedResult<AccountDetailEntity> failedTypedResult:
                        return new FailedResult(failedTypedResult.Error);
                    case NotFoundTypedResult<AccountDetailEntity> notFoundTypedResult:
                        return new FailedResult(new Exception("Account Not Found"));
                    case SuccessfulTypedResult<AccountDetailEntity> successfulTypedResult:
                        foundAccount = successfulTypedResult.Value;
                        break;
                    default:
                        return new FailedResult(new ArgumentOutOfRangeException(nameof(foundAccountResult)));
                }

                var balances = foundAccount.Balances.ToList();
                var closingBookedBalance = balances.First(b => b.BalanceType == BalanceTypeEnum.closingBooked);

                closingBookedBalance.BalanceAmount = closingBookedBalance.BalanceAmount.Add(newCreditTransaction.TransactionAmount);
                closingBookedBalance.LastChangeDateTime = dateTimeNow;
                closingBookedBalance.LastCommittedTransaction = newCreditTransaction.TransactionId;

                var expectedBalance = balances.FirstOrDefault(b => b.BalanceType == BalanceTypeEnum.expected);
                if (expectedBalance != null)
                {
                    if (expectedBalance.BalanceAmount.AreEqual(closingBookedBalance.BalanceAmount))
                    {
                        balances.Remove(expectedBalance);
                    }
                }

                foundAccount.Balances = balances;
                foundAccount.LastModifiedDate = dateTimeNow;

                _accounts.FindOneAndReplace(a => a.ResourceId == foundAccount.ResourceId, foundAccount);
                return new SuccessResult();
            }
            catch (Exception e)
            {
                return new FailedResult(e);
            }
        }

        public async Task<Result> UpdateBalance_BookDebit(DebitTransactionEntity newDebitTransaction)
        {
            try
            {
                var dateTimeNow = DateTimeOffset.UtcNow;
                var foundAccountResult = await GetEntity(newDebitTransaction.AssociatedAccountId);
                AccountDetailEntity foundAccount;
                switch (foundAccountResult)
                {
                    case BadRequestTypedResult<AccountDetailEntity> badRequestTypedResult:
                        return new FailedResult(new Exception(badRequestTypedResult.Problem.Message));
                    case FailedTypedResult<AccountDetailEntity> failedTypedResult:
                        return new FailedResult(failedTypedResult.Error);
                    case NotFoundTypedResult<AccountDetailEntity> _:
                        return new FailedResult(new Exception("Account Not Found"));
                    case SuccessfulTypedResult<AccountDetailEntity> successfulTypedResult:
                        foundAccount = successfulTypedResult.Value;
                        break;
                    default:
                        return new FailedResult(new ArgumentOutOfRangeException(nameof(foundAccountResult)));
                }

                var balances = foundAccount.Balances.ToList();
                var closingBookedBalance = balances.First(b => b.BalanceType == BalanceTypeEnum.closingBooked);

                closingBookedBalance.BalanceAmount = closingBookedBalance.BalanceAmount.Subtract(newDebitTransaction.TransactionAmount);
                closingBookedBalance.LastChangeDateTime = dateTimeNow;
                closingBookedBalance.LastCommittedTransaction = newDebitTransaction.TransactionId;

                var expectedBalance = balances.FirstOrDefault(b => b.BalanceType == BalanceTypeEnum.expected);
                if (expectedBalance != null)
                {
                    if (expectedBalance.BalanceAmount.AreEqual(closingBookedBalance.BalanceAmount))
                    {
                        balances.Remove(expectedBalance);
                    }
                }

                foundAccount.Balances = balances;
                foundAccount.LastModifiedDate = dateTimeNow;

                _accounts.FindOneAndReplace(a => a.ResourceId == foundAccount.ResourceId, foundAccount);
                return new SuccessResult();
            }
            catch (Exception e)
            {
                return new FailedResult(e);
            }
        }
    }
}