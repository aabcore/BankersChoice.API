using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using BankersChoice.API.Models;
using BankersChoice.API.Models.ApiDtos.Account;
using BankersChoice.API.Models.ApiDtos.Transaction;
using BankersChoice.API.Models.Entities.Account;
using BankersChoice.API.Models.Entities.Transaction;
using BankersChoice.API.Results;
using BankersChoice.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BankersChoice.API.Controllers
{
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [Route("api/Accounts/{resourceId:Guid}/[controller]")]
    [ApiController]
    public class TransactionsController : ControllerBase
    {
        private TransactionService TransactionService { get; }
        private AccountService AccountService { get; }

        public TransactionsController(TransactionService transactionService, AccountService accountService)
        {
            TransactionService = transactionService;
            AccountService = accountService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(AccountTransactionsListOutDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestOutDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllTransactions(Guid resourceId, DateTimeOffset? dateFrom = null,
            DateTimeOffset? dateTo = null, BookingStatusEnum? bookingStatus = null)
        {
            TransactionService.TransactionFilter filterTransactions = null;
            if (dateFrom.HasValue || dateTo.HasValue || bookingStatus.HasValue)
            {
                filterTransactions = new TransactionService.TransactionFilter()
                {
                    DateFrom = dateFrom,
                    DateTo = dateTo,
                    BookingStatus = bookingStatus
                };
            }

            var getTransactionsResult = await TransactionService.SearchInAccount(resourceId, filterTransactions);
            switch (getTransactionsResult)
            {
                case BadRequestTypedResult<AccountTransactionsListOutDto> badRequestTypedResult:
                    return BadRequest(badRequestTypedResult.Problem);
                case FailedTypedResult<AccountTransactionsListOutDto> failedTypedResult:
                    return StatusCode(StatusCodes.Status500InternalServerError, failedTypedResult.Error);
                case NotFoundTypedResult<AccountTransactionsListOutDto> _:
                    return NotFound();
                case SuccessfulTypedResult<AccountTransactionsListOutDto> successfulTypedResult:
                    return Ok(successfulTypedResult.Value);
                default:
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new ArgumentOutOfRangeException(nameof(getTransactionsResult)));
            }
        }

        [HttpGet]
        [Route("{transactionId:Guid}")]
        [ProducesResponseType(typeof(AccountTransactionsListOutDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetSingleTransaction(Guid resourceId, Guid transactionId)
        {
            var getTransactionResult = await TransactionService.GetTransaction(resourceId, transactionId);

            switch (getTransactionResult)
            {
                case BadRequestTypedResult<TransactionDetailsOutDto> badRequestTypedResult:
                    return BadRequest(badRequestTypedResult.Problem);
                case FailedTypedResult<TransactionDetailsOutDto> failedTypedResult:
                    return StatusCode(StatusCodes.Status500InternalServerError, failedTypedResult.Error);
                case NotFoundTypedResult<TransactionDetailsOutDto> _:
                    return NotFound();
                case SuccessfulTypedResult<TransactionDetailsOutDto> successfulTypedResult:
                    return Ok(successfulTypedResult.Value);
                default:
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new ArgumentOutOfRangeException(nameof(getTransactionResult)));
            }
        }

        [HttpPost]
        [Route("createDebit")]
        [ProducesResponseType(typeof(DebitTransactionDetailsOutDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> NewDebitTransaction(Guid resourceId, DebitNewTransactionInDto newDebitTransactionInDto)
        {

            var lockCheck = await CheckLockOnAccount(resourceId, newDebitTransactionInDto);
            switch (lockCheck)
            {
                case Continue_EarlyReturnResult<IActionResult> _:
                    break;
                case ReturnEarly_EarlyReturnResult<IActionResult> returnEarlyEarlyReturnResult:
                    return returnEarlyEarlyReturnResult.EarlyReturnValue;
                default:
                    return StatusCode(StatusCodes.Status500InternalServerError, new ArgumentOutOfRangeException(nameof(lockCheck)));
            }
            var newTransactionResult = await TransactionService.NewDebitTransaction(resourceId, newDebitTransactionInDto);

            switch (newTransactionResult)
            {
                case BadRequestLockableResult<TransactionDetailsOutDto> badRequestLockableResult:
                    return BadRequest(badRequestLockableResult.Problem);
                case FailedLockableResult<TransactionDetailsOutDto> failedLockableResult:
                    return StatusCode(StatusCodes.Status500InternalServerError, failedLockableResult.Error);
                case NotFoundLockableResult<TransactionDetailsOutDto> notFoundLockableResult:
                    return NotFound();
                case LockedLockableResult<TransactionDetailsOutDto> lockedLockableResult:
                    return BadRequest(BadRequestOutDto.AccountLockedByAnotherUser);
                case NotLockedLockableResult<TransactionDetailsOutDto> notLockedLockableResult:
                    return BadRequest(BadRequestOutDto.AccountNotLocked);
                case SuccessfulLockableResult<TransactionDetailsOutDto> successfulLockableResult:
                    return Ok(successfulLockableResult.Value);
                default:
                    return StatusCode(StatusCodes.Status500InternalServerError, new ArgumentOutOfRangeException(nameof(newTransactionResult)));
            }
        }

        [HttpPost]
        [Route("createCredit")]
        [ProducesResponseType(typeof(CreditTransactionDetailsOutDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> NewCreditTransaction(Guid resourceId, CreditNewTransactionInDto newCreditTransactionInDto)
        {
            var lockCheck = await CheckLockOnAccount(resourceId, newCreditTransactionInDto);
            switch (lockCheck)
            {
                case Continue_EarlyReturnResult<IActionResult> _:
                    break;
                case ReturnEarly_EarlyReturnResult<IActionResult> returnEarlyEarlyReturnResult:
                    return returnEarlyEarlyReturnResult.EarlyReturnValue;
                default:
                    return StatusCode(StatusCodes.Status500InternalServerError, new ArgumentOutOfRangeException(nameof(lockCheck)));
            }

            var newTransactionResult = await TransactionService.NewCreditTransaction(resourceId, newCreditTransactionInDto);

            switch (newTransactionResult)
            {
                case BadRequestLockableResult<TransactionDetailsOutDto> badRequestLockableResult:
                    return BadRequest(badRequestLockableResult.Problem);
                case FailedLockableResult<TransactionDetailsOutDto> failedLockableResult:
                    return StatusCode(StatusCodes.Status500InternalServerError, failedLockableResult.Error);
                case NotFoundLockableResult<TransactionDetailsOutDto> _:
                    return NotFound();
                case LockedLockableResult<TransactionDetailsOutDto> _:
                    return BadRequest(BadRequestOutDto.AccountLockedByAnotherUser);
                case NotLockedLockableResult<TransactionDetailsOutDto> _:
                    return BadRequest(BadRequestOutDto.AccountNotLocked);
                case SuccessfulLockableResult<TransactionDetailsOutDto> successfulLockableResult:
                    return Ok(successfulLockableResult.Value);
                default:
                    return StatusCode(StatusCodes.Status500InternalServerError, new ArgumentOutOfRangeException(nameof(newTransactionResult)));
            }
        }

        [HttpPost]
        [Route("book")]
        [ProducesResponseType(typeof(TransactionDetailsOutDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> BookTransaction(Guid resourceId, BookTransactionInDto bookTransactionIn)
        {
            var lockCheck = await CheckLockOnAccount(resourceId, bookTransactionIn);
            switch (lockCheck)
            {
                case Continue_EarlyReturnResult<IActionResult> _:
                    break;
                case ReturnEarly_EarlyReturnResult<IActionResult> returnEarlyEarlyReturnResult:
                    return returnEarlyEarlyReturnResult.EarlyReturnValue;
                default:
                    return StatusCode(StatusCodes.Status500InternalServerError, new ArgumentOutOfRangeException(nameof(lockCheck)));
            }

            var bookTransactionResult = await TransactionService.BookTransaction(resourceId, bookTransactionIn.TransactionId);

            switch (bookTransactionResult)
            {
                case BadRequestLockableResult<TransactionDetailsOutDto> badRequestLockableResult:
                    return BadRequest(badRequestLockableResult.Problem);
                case FailedLockableResult<TransactionDetailsOutDto> failedLockableResult:
                    return StatusCode(StatusCodes.Status500InternalServerError, failedLockableResult.Error);
                case NotFoundLockableResult<TransactionDetailsOutDto> _:
                    return NotFound();
                case LockedLockableResult<TransactionDetailsOutDto> _:
                    return BadRequest(BadRequestOutDto.AccountLockedByAnotherUser);
                case NotLockedLockableResult<TransactionDetailsOutDto> _:
                    return BadRequest(BadRequestOutDto.AccountNotLocked);
                case SuccessfulLockableResult<TransactionDetailsOutDto> successfulLockableResult:
                    return Ok(successfulLockableResult.Value);
                default:
                    return StatusCode(StatusCodes.Status500InternalServerError, new ArgumentOutOfRangeException(nameof(bookTransactionResult)));
            }
        }

        private async Task<EarlyReturnResult<IActionResult>> CheckLockOnAccount(Guid resourceId, IUpdateWithSecret updateWithSecret)
        {
            var lockCheckResult = await AccountService.CheckLock(resourceId, updateWithSecret.LockSecret);
            switch (lockCheckResult)
            {
                case BadRequestTypedResult<AccountService.LockCheck> badRequestTypedResult:
                    return new ReturnEarly_EarlyReturnResult<IActionResult>(BadRequest(badRequestTypedResult.Problem));
                case FailedTypedResult<AccountService.LockCheck> failedTypedResult:
                    return new ReturnEarly_EarlyReturnResult<IActionResult>(StatusCode(StatusCodes.Status500InternalServerError, failedTypedResult.Error));
                case NotFoundTypedResult<AccountService.LockCheck> notFoundTypedResult:
                    return new ReturnEarly_EarlyReturnResult<IActionResult>(BadRequest(BadRequestOutDto.AccountNotFound));
                case SuccessfulTypedResult<AccountService.LockCheck> successfulTypedResult:
                    if (!successfulTypedResult.Value.AccountLocked)
                    {
                        return new ReturnEarly_EarlyReturnResult<IActionResult>(BadRequest(BadRequestOutDto.AccountNotLocked));
                    }

                    if (!successfulTypedResult.Value.CorrectSecret)
                    {
                        return new ReturnEarly_EarlyReturnResult<IActionResult>(BadRequest(BadRequestOutDto.AccountLockedByAnotherUser));
                    }

                    return new Continue_EarlyReturnResult<IActionResult>();
                default:
                    return new ReturnEarly_EarlyReturnResult<IActionResult>(StatusCode(StatusCodes.Status500InternalServerError, new ArgumentOutOfRangeException(nameof(lockCheckResult))));
            }

        }
    }


    public interface IUpdateWithSecret
    {
        public string LockSecret { get; set; }
    }

    public class NewTransactionInDto: IUpdateWithSecret
    {
        public string CheckId { get; set; }

        [Required]
        public AmountDto TransactionAmount { get; set; }

        public string LockSecret { get; set; }
    }

    public class CreditNewTransactionInDto : NewTransactionInDto
    {
        [Required]
        public string CreditorName { get; set; }
        [Required]
        public AccountReferenceInDto CreditorAccount { get; set; }

        public DateTimeOffset? ValueDate { get; set; }
    }

    public class DebitNewTransactionInDto : NewTransactionInDto
    {
        [Required]
        public string DebtorName { get; set; }
        [Required]
        public AccountReferenceInDto DebtorAccount { get; set; }
    }

    public class BookTransactionInDto: IUpdateWithSecret
    {
        public Guid TransactionId { get; set; }
        public string LockSecret { get; set; }
    }

    public class AccountReferenceInDto
    {
        [Required]
        public string Pan { get; set; }

        public RoutingNumbersOutDto RoutingNumbers { get; set; }
        public CurrencyEnum Currency { get; set; }
        public string Msisdn { get; set; }
    }
}