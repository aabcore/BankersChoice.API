using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankersChoice.API.Controllers;
using BankersChoice.API.Models;
using BankersChoice.API.Models.ApiDtos.Transaction;
using BankersChoice.API.Models.Entities.Account;
using BankersChoice.API.Models.Entities.Transaction;
using BankersChoice.API.Results;
using MongoDB.Driver;

namespace BankersChoice.API.Services
{
    public class TransactionService : DbService
    {
        private AccountService AccountService { get; }
        private readonly IMongoCollection<TransactionEntity> TransactionCollection;

        public TransactionService(DatabaseSettings dbSettings, AccountService accountService)
        {
            AccountService = accountService;
            var database = BuildDatabaseClient(dbSettings);
            TransactionCollection = database.GetCollection<TransactionEntity>(dbSettings.TransactionsCollectionName);
        }

        public async Task<TypedResult<AccountTransactionsListOutDto>> SearchInAccount(Guid accountId,
            TransactionFilter transactionFilter)
        {
            try
            {
                var foundAccountResult = await AccountService.GetEntity(accountId);

                AccountDetailEntity foundAccount;
                switch (foundAccountResult)
                {
                    case BadRequestTypedResult<AccountDetailEntity> badRequestTypedResult:
                        return new BadRequestTypedResult<AccountTransactionsListOutDto>(badRequestTypedResult.Problem);
                    case FailedTypedResult<AccountDetailEntity> failedTypedResult:
                        return new FailedTypedResult<AccountTransactionsListOutDto>(failedTypedResult.Error);
                    case NotFoundTypedResult<AccountDetailEntity> notFoundTypedResult:
                        return new BadRequestTypedResult<AccountTransactionsListOutDto>(
                            BadRequestOutDto.AccountNotFound);
                    case SuccessfulTypedResult<AccountDetailEntity> successfulTypedResult:
                        foundAccount = successfulTypedResult.Value;
                        break;
                    default:
                        return new FailedTypedResult<AccountTransactionsListOutDto>(
                            new ArgumentOutOfRangeException(nameof(foundAccountResult)));
                }

                var searchFilters = new List<FilterDefinition<TransactionEntity>>();
                if (transactionFilter != null)
                {
                    if (transactionFilter.DateFrom.HasValue)
                    {
                        searchFilters.Add(Builders<TransactionEntity>.Filter.And(
                            Builders<TransactionEntity>.Filter.Eq(t => t.BookingStatus, BookingStatusEnum.BOOKED),
                            Builders<TransactionEntity>.Filter.Gte(t => t.BookingDate, transactionFilter.DateFrom)));
                        searchFilters.Add(Builders<TransactionEntity>.Filter.And(
                            Builders<TransactionEntity>.Filter.Eq(t => t.BookingStatus, BookingStatusEnum.PENDING),
                            Builders<TransactionEntity>.Filter.Gte(t => t.EntryDate, transactionFilter.DateFrom)));
                    }

                    if (transactionFilter.DateTo.HasValue)
                    {
                        searchFilters.Add(Builders<TransactionEntity>.Filter.And(
                            Builders<TransactionEntity>.Filter.Eq(t => t.BookingStatus, BookingStatusEnum.BOOKED),
                            Builders<TransactionEntity>.Filter.Lt(t => t.BookingDate, transactionFilter.DateFrom)));
                        searchFilters.Add(Builders<TransactionEntity>.Filter.And(
                            Builders<TransactionEntity>.Filter.Eq(t => t.BookingStatus, BookingStatusEnum.PENDING),
                            Builders<TransactionEntity>.Filter.Lt(t => t.EntryDate, transactionFilter.DateFrom)));
                    }

                    if (transactionFilter.BookingStatus.HasValue)
                    {
                        searchFilters.Add(
                            Builders<TransactionEntity>.Filter.Eq(t => t.BookingStatus,
                                transactionFilter.BookingStatus));
                    }
                }

                // Add the associated account id. Kinda important.
                searchFilters.Add(Builders<TransactionEntity>.Filter.Eq(t => t.AssociatedAccountId, accountId));

                var foundTransactions = TransactionCollection
                    .Find(Builders<TransactionEntity>.Filter.And(searchFilters)).ToList();
                var accountTransactionsListOutDto =
                    AccountTransactionsListOutDto.BuildFromEntities(foundAccount, foundTransactions);
                if (transactionFilter?.BookingStatus != null)
                {
                    if (transactionFilter.BookingStatus == BookingStatusEnum.PENDING)
                    {
                        accountTransactionsListOutDto.Transactions.Booked = null;
                    }

                    if (transactionFilter.BookingStatus == BookingStatusEnum.BOOKED)
                    {
                        accountTransactionsListOutDto.Transactions.Pending = null;
                    }
                }

                return new SuccessfulTypedResult<AccountTransactionsListOutDto>(
                    accountTransactionsListOutDto);
            }
            catch (Exception e)
            {
                return new FailedTypedResult<AccountTransactionsListOutDto>(e);
            }
        }

        public class TransactionFilter
        {
            public DateTimeOffset? DateFrom { get; set; }
            public DateTimeOffset? DateTo { get; set; }
            public BookingStatusEnum? BookingStatus { get; set; }
        }

        public async Task<TypedResult<TransactionDetailsOutDto>> GetTransaction(Guid accountId, Guid transactionId)
        {
            try
            {
                var foundTransaction =
                    (await TransactionCollection.FindAsync(t =>
                        t.TransactionId == transactionId && t.AssociatedAccountId == accountId)).FirstOrDefault();

                if (foundTransaction == null)
                {
                    return new NotFoundTypedResult<TransactionDetailsOutDto>();
                }

                return new SuccessfulTypedResult<TransactionDetailsOutDto>(
                    TransactionDetailsOutDto.EntityToOutDto(foundTransaction));
            }
            catch (Exception e)
            {
                return new FailedTypedResult<TransactionDetailsOutDto>(e);
            }
        }

        public async Task<LockableResult<TransactionDetailsOutDto>> NewDebitTransaction(Guid accountId,
            DebitNewTransactionInDto newTransactionInDto)
        {
            try
            {
                var foundAccountResult = await AccountService.GetEntity(accountId);
                AccountDetailEntity foundAccount;
                switch (foundAccountResult)
                {
                    case BadRequestTypedResult<AccountDetailEntity> badRequestTypedResult:
                        return new BadRequestLockableResult<TransactionDetailsOutDto>(badRequestTypedResult.Problem);
                    case FailedTypedResult<AccountDetailEntity> failedTypedResult:
                        return new FailedLockableResult<TransactionDetailsOutDto>(failedTypedResult.Error);
                    case NotFoundTypedResult<AccountDetailEntity> _:
                        return new BadRequestLockableResult<TransactionDetailsOutDto>(BadRequestOutDto.AccountNotFound);
                    case SuccessfulTypedResult<AccountDetailEntity> successfulTypedResult:
                        foundAccount = successfulTypedResult.Value;
                        break;
                    default:
                        return new FailedLockableResult<TransactionDetailsOutDto>(
                            new ArgumentOutOfRangeException(nameof(foundAccountResult)));
                }

                if (foundAccount.Status != AccountStatusEnum.enabled)
                {
                    return new BadRequestLockableResult<TransactionDetailsOutDto>(BadRequestOutDto.AccountNotEnabled);
                }

                if (foundAccount.Currency != newTransactionInDto.TransactionAmount.Currency)
                {
                    return new BadRequestLockableResult<TransactionDetailsOutDto>(BadRequestOutDto.WrongCurrencyType);
                }

                var newDebitTransaction = new DebitTransactionEntity
                {
                    TransactionId = Guid.NewGuid(),
                    AssociatedAccountId = accountId,
                    TransactionType = TransactionTypeEnum.DEBIT,
                    CheckId = newTransactionInDto.CheckId,
                    TransactionAmount = newTransactionInDto.TransactionAmount.ToEntity(),
                    EntryDate = DateTimeOffset.UtcNow,
                    BookingDate = null,
                    BookingStatus = BookingStatusEnum.PENDING,
                    DebtorName = newTransactionInDto.DebtorName,
                    DebtorAccount = new AccountReferenceEntity()
                    {
                        Msisdn = newTransactionInDto.DebtorAccount.Msisdn,
                        Pan = newTransactionInDto.DebtorAccount.Pan,
                        Currency = newTransactionInDto.DebtorAccount.Currency,
                        RoutingNumbers = new AccountReferenceEntity.RoutingNumbersEntity()
                        {
                            Wire = newTransactionInDto.DebtorAccount.RoutingNumbers.Wire,
                            Ach = newTransactionInDto.DebtorAccount.RoutingNumbers.Ach
                        }
                    }
                };

                await TransactionCollection.InsertOneAsync(newDebitTransaction);
                var updateAccountBalance = await AccountService.UpdateBalance_NewDebit(newDebitTransaction);
                if (updateAccountBalance is SuccessResult)
                {
                    return new SuccessfulLockableResult<TransactionDetailsOutDto>(
                        TransactionDetailsOutDto.EntityToOutDto(newDebitTransaction));
                }
                else
                {
                    var failedResult = updateAccountBalance as FailedResult;
                    TransactionCollection.FindOneAndDelete(t => t.TransactionId == newDebitTransaction.TransactionId);
                    return new FailedLockableResult<TransactionDetailsOutDto>(
                        failedResult?.Exception ?? new Exception("Failed to update balances."));
                }
            }
            catch (Exception e)
            {
                return new FailedLockableResult<TransactionDetailsOutDto>(e);
            }
        }

        public async Task<LockableResult<TransactionDetailsOutDto>> NewCreditTransaction(Guid accountId,
            CreditNewTransactionInDto newCreditTransactionInDto)
        {
            try
            {
                var foundAccountResult = await AccountService.GetEntity(accountId);

                AccountDetailEntity foundAccount;
                switch (foundAccountResult)
                {
                    case BadRequestTypedResult<AccountDetailEntity> badRequestTypedResult:
                        return new BadRequestLockableResult<TransactionDetailsOutDto>(badRequestTypedResult.Problem);
                    case FailedTypedResult<AccountDetailEntity> failedTypedResult:
                        return new FailedLockableResult<TransactionDetailsOutDto>(failedTypedResult.Error);
                    case NotFoundTypedResult<AccountDetailEntity> _:
                        return new BadRequestLockableResult<TransactionDetailsOutDto>(BadRequestOutDto.AccountNotFound);
                    case SuccessfulTypedResult<AccountDetailEntity> successfulTypedResult:
                        foundAccount = successfulTypedResult.Value;
                        break;
                    default:
                        return new FailedLockableResult<TransactionDetailsOutDto>(
                            new ArgumentOutOfRangeException(nameof(foundAccountResult)));
                }

                if (foundAccount.Status != AccountStatusEnum.enabled)
                {
                    return new BadRequestLockableResult<TransactionDetailsOutDto>(BadRequestOutDto.AccountNotEnabled);
                }

                if (foundAccount.Currency != newCreditTransactionInDto.TransactionAmount.Currency)
                {
                    return new BadRequestLockableResult<TransactionDetailsOutDto>(BadRequestOutDto.WrongCurrencyType);
                }

                var newCreditTransaction = new CreditTransactionEntity()
                {
                    TransactionId = Guid.NewGuid(),
                    AssociatedAccountId = accountId,
                    TransactionType = TransactionTypeEnum.CREDIT,
                    CheckId = newCreditTransactionInDto.CheckId,
                    TransactionAmount = newCreditTransactionInDto.TransactionAmount.ToEntity(),
                    EntryDate = DateTimeOffset.UtcNow,
                    BookingDate = null,
                    BookingStatus = BookingStatusEnum.PENDING,
                    CreditorName = newCreditTransactionInDto.CreditorName,
                    CreditorAccount = new AccountReferenceEntity()
                    {
                        Msisdn = newCreditTransactionInDto.CreditorAccount.Msisdn,
                        Pan = newCreditTransactionInDto.CreditorAccount.Pan,
                        Currency = newCreditTransactionInDto.CreditorAccount.Currency,
                        RoutingNumbers = new AccountReferenceEntity.RoutingNumbersEntity()
                        {
                            Wire = newCreditTransactionInDto.CreditorAccount.RoutingNumbers.Wire,
                            Ach = newCreditTransactionInDto.CreditorAccount.RoutingNumbers.Ach
                        }
                    },
                    ValueDate = newCreditTransactionInDto.ValueDate
                };

                await TransactionCollection.InsertOneAsync(newCreditTransaction);
                var updateAccountBalance = await AccountService.UpdateBalance_NewCredit(newCreditTransaction);
                if (updateAccountBalance is SuccessResult)
                {
                    return new SuccessfulLockableResult<TransactionDetailsOutDto>(
                        TransactionDetailsOutDto.EntityToOutDto(newCreditTransaction));
                }
                else
                {
                    var failedResult = updateAccountBalance as FailedResult;
                    TransactionCollection.FindOneAndDelete(t => t.TransactionId == newCreditTransaction.TransactionId);
                    return new FailedLockableResult<TransactionDetailsOutDto>(
                        failedResult?.Exception ?? new Exception("Failed to update balances."));
                }
            }
            catch (Exception e)
            {
                return new FailedLockableResult<TransactionDetailsOutDto>(e);
            }
        }

        public async Task<LockableResult<TransactionDetailsOutDto>> BookTransaction(Guid accountId, Guid creditTransactionId)
        {
            try
            {
                var foundAccountResult = await AccountService.GetEntity(accountId);
                switch (foundAccountResult)
                {
                    case BadRequestTypedResult<AccountDetailEntity> badRequestTypedResult:
                        return new BadRequestLockableResult<TransactionDetailsOutDto>(badRequestTypedResult.Problem);
                    case FailedTypedResult<AccountDetailEntity> failedTypedResult:
                        return new FailedLockableResult<TransactionDetailsOutDto>(failedTypedResult.Error);
                    case NotFoundTypedResult<AccountDetailEntity> _:
                        return new BadRequestLockableResult<TransactionDetailsOutDto>(BadRequestOutDto.AccountNotFound);
                    case SuccessfulTypedResult<AccountDetailEntity> _:
                        break;
                    default:
                        return new FailedLockableResult<TransactionDetailsOutDto>(
                            new ArgumentOutOfRangeException(nameof(foundAccountResult)));
                }

                var updatedTransaction = TransactionCollection.FindOneAndUpdate<TransactionEntity>(
                    t => t.TransactionId == creditTransactionId && t.AssociatedAccountId == accountId && t.BookingStatus == BookingStatusEnum.PENDING,
                    Builders<TransactionEntity>.Update.Set(t => t.BookingStatus, BookingStatusEnum.BOOKED),
                    GetEntityAfterUpdateOption<TransactionEntity>());

                if (updatedTransaction == null)
                {
                    return new BadRequestLockableResult<TransactionDetailsOutDto>(
                        "Failed to find matching transaction to book.");
                }

                Result accountBalanceUpdateResult;
                switch (updatedTransaction)
                {
                    case CreditTransactionEntity creditTransactionEntity:
                        accountBalanceUpdateResult = await AccountService.UpdateBalance_BookCredit(creditTransactionEntity);
                        break;
                    case DebitTransactionEntity debitTransactionEntity:
                        accountBalanceUpdateResult = await AccountService.UpdateBalance_BookDebit(debitTransactionEntity);
                        break;
                    default:
                        accountBalanceUpdateResult = new FailedResult(new ArgumentOutOfRangeException(nameof(updatedTransaction)));
                        break;
                }

                if (accountBalanceUpdateResult is SuccessResult)
                {
                    return new SuccessfulLockableResult<TransactionDetailsOutDto>(
                        TransactionDetailsOutDto.EntityToOutDto(updatedTransaction));
                }
                else
                {
                    var failedResult = accountBalanceUpdateResult as FailedResult;
                    TransactionCollection.FindOneAndUpdate(t => t.TransactionId == updatedTransaction.TransactionId,
                        Builders<TransactionEntity>.Update.Set(t => t.BookingStatus, BookingStatusEnum.PENDING));
                    return new FailedLockableResult<TransactionDetailsOutDto>(
                        failedResult?.Exception ?? new Exception("Failed to update balances."));
                }
            }
            catch (Exception e)
            {
                return new FailedLockableResult<TransactionDetailsOutDto>(e);
            }
        }
    }
}