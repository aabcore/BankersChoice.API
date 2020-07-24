using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using BankersChoice.API.Models;
using BankersChoice.API.Models.ApiDtos;
using BankersChoice.API.Models.ApiDtos.Account;
using BankersChoice.API.Models.Entities;
using BankersChoice.API.Models.Entities.Account;
using BankersChoice.API.Results;
using BankersChoice.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson.Serialization.Attributes;

namespace BankersChoice.API.Controllers
{
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [Route("api/[controller]")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        private AccountService _accountService;

        public AccountsController(AccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<AccountDetailsOutDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllAccounts(AccountStatusEnum? status = null,
            ExternalCashAccountType1Code? cashAccountType = null, UsageEnum? usage = null,
            DateTimeOffset? lastModifiedBefore = null, DateTimeOffset? lastModifiedAfter = null)
        {
            var getFilter = new AccountService.GetFilter()
            {
                lastModifiedBefore = lastModifiedBefore,
                lastModifiedAfter = lastModifiedAfter,
                status = status,
                cashAccountType = cashAccountType,
                usage = usage
            };
            var accounts = await _accountService.Get(getFilter);
            return Ok(accounts);
        }

        [HttpGet]
        [ProducesResponseType(typeof(AccountDetailsOutDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("{resourceId:Guid}")]
        public async Task<IActionResult> GetAccount(Guid resourceId)
        {
            var account = await _accountService.Get(resourceId);
            if (account != null)
            {
                return Ok(account);
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(AccountDetailsOutDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestOutDto), StatusCodes.Status400BadRequest)]

        public async Task<IActionResult> AddNewAccount(AccountNewDto account)
        {
            var createdAccountResult = await _accountService.Create(account);

            switch (createdAccountResult)
            {
                case BadRequestLockableResult<AccountDetailsOutDto> badRequestLockableResult:
                    return BadRequest(badRequestLockableResult.Problem);
                case FailedLockableResult<AccountDetailsOutDto> failedLockableResult:
                    return StatusCode(StatusCodes.Status500InternalServerError, failedLockableResult.Error);
                case SuccessfulLockableResult<AccountDetailsOutDto> successfulLockableResult:
                    return Ok(successfulLockableResult.Value);
                default:
                    throw new ArgumentOutOfRangeException(nameof(createdAccountResult));
            }
        }

        [HttpPatch]
        [ProducesResponseType(typeof(AccountDetailsOutDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(BadRequestOutDto), StatusCodes.Status400BadRequest)]
        [Route("{resourceId:Guid}")]
        public async Task<IActionResult> UpdateAccount(Guid resourceId,
            [FromBody] AccountUpdateDto accountUpdateDto)
        {
            var updatedAccountResult = await _accountService.Update(resourceId, accountUpdateDto);

            switch (updatedAccountResult)
            {
                case FailedLockableResult<AccountDetailsOutDto> failedLockableResult:
                    return StatusCode(StatusCodes.Status500InternalServerError, failedLockableResult.Error);
                case LockedLockableResult<AccountDetailsOutDto> _:
                    return BadRequest(BadRequestOutDto.AccountLockedByAnotherUser);
                case NotLockedLockableResult<AccountDetailsOutDto> _:
                    return BadRequest(BadRequestOutDto.AccountNotLocked);
                case NotFoundLockableResult<AccountDetailsOutDto> _:
                    return StatusCode(StatusCodes.Status404NotFound);
                case SuccessfulLockableResult<AccountDetailsOutDto> successfulLockableResult:
                    return Ok(successfulLockableResult.Value);
                default:
                    return StatusCode(StatusCodes.Status500InternalServerError, new ArgumentOutOfRangeException(nameof(updatedAccountResult)));
            }
        }

        [HttpPost]
        [Route("{resourceId:Guid}/lock")]
        [ProducesResponseType(typeof(LockAccountOutDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LockAccount(Guid resourceId, [FromBody] LockAccountInDto lockAccountIn)
        {
            var lockAccountResult = await _accountService.Lock(resourceId, lockAccountIn.UserId);

            switch (lockAccountResult)
            {
                case FailedLockableResult<LockAccountOutDto> failedLockableResult:
                    return StatusCode(StatusCodes.Status500InternalServerError, failedLockableResult.Error);
                case LockedLockableResult<LockAccountOutDto> lockedLockableResult:
                    return BadRequest("Account is locked by another user.");
                case NotFoundLockableResult<LockAccountOutDto> _:
                    return NotFound();
                case SuccessfulLockableResult<LockAccountOutDto> successfulLockableResult:
                    return Ok(successfulLockableResult.Value);
                case BadRequestLockableResult<LockAccountOutDto> badRequestLockableResult:
                    return BadRequest(badRequestLockableResult.Problem);
                default:
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new ArgumentOutOfRangeException(nameof(lockAccountResult)));
            }
        }

        [HttpPost]
        [Route("{resourceId:Guid}/unlock")]
        [ProducesResponseType(typeof(LockAccountOutDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UnlockAccount(Guid resourceId, [FromBody] UnlockAccountInDto unlockAccountIn)
        {
            var unlockAccountResult = await _accountService.Unlock(resourceId, unlockAccountIn.LockSecret);
            switch (unlockAccountResult)
            {
                case FailedTypedResult<UnlockAccountOutDto> failedTypedResult:
                    return StatusCode(StatusCodes.Status500InternalServerError, failedTypedResult.Error);
                case NotFoundTypedResult<UnlockAccountOutDto> _:
                    return NotFound();
                case SuccessfulTypedResult<UnlockAccountOutDto> successfulTypedResult:
                    return Ok(successfulTypedResult.Value);
                default:
                    return StatusCode(500, new ArgumentOutOfRangeException(nameof(unlockAccountResult)));
            }
        }

        [HttpPost]
        [Route("{resourceId:Guid}/forceUnlock")]
        [ProducesResponseType(typeof(LockAccountOutDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ForceUnlockAccount(Guid resourceId)
        {
            var unlockAccountResult = await _accountService.ForceUnlock(resourceId);
            switch (unlockAccountResult)
            {
                case FailedTypedResult<UnlockAccountOutDto> failedTypedResult:
                    return StatusCode(StatusCodes.Status500InternalServerError, failedTypedResult.Error);
                case NotFoundTypedResult<UnlockAccountOutDto> _:
                    return NotFound();
                case SuccessfulTypedResult<UnlockAccountOutDto> successfulTypedResult:
                    return Ok(successfulTypedResult.Value);
                default:
                    return StatusCode(500, new ArgumentOutOfRangeException(nameof(unlockAccountResult)));
            }
        }
    }
}