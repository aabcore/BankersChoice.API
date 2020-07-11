using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using BankersChoice.API.Models.Account;
using BankersChoice.API.Models.Entities;
using BankersChoice.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson.Serialization.Attributes;

namespace BankersChoice.API.Controllers
{
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
        [ProducesResponseType(typeof(IEnumerable<AccountDetailsOutDto>), 200)]
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
        [ProducesResponseType(typeof(AccountDetailsOutDto), 200)]
        [ProducesResponseType(404)]
        [Route("{accountReferenceId:Guid}")]
        public async Task<IActionResult> GetAccount(Guid accountReferenceId)
        {
            var account = await _accountService.Get(accountReferenceId);
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
        [ProducesResponseType(typeof(AccountDetailsOutDto), 200)]
        public async Task<IActionResult> AddNewAccount(AccountNewDto account)
        {
            var createdAccount = await _accountService.Create(account);
            return Ok(createdAccount);
        }

        [HttpPatch]
        [ProducesResponseType(typeof(AccountDetailsOutDto), 200)]
        [ProducesResponseType(404)]
        [Route("{accountReferenceId:Guid}")]
        public async Task<IActionResult> UpdateAccount(Guid accountReferenceId,
            [FromBody] AccountUpdateDto accountUpdateDto)
        {
            var updatedAccount = await _accountService.Update(accountReferenceId, accountUpdateDto);

            if (updatedAccount != null)
            {
                return Ok(updatedAccount);
            }
            else
            {
                return NotFound();
            }
        }
    }

    public class AccountUpdateDto
    {
        public string Name { get; set; }
        public AccountStatusEnum? Status { get; set; }
        public string Msisdn { get; set; }
    }

    public class AccountNewDto
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Product { get; set; }

        public ExternalCashAccountType1Code CashAccountType { get; set; } = ExternalCashAccountType1Code.TRAN;
        public AccountStatusEnum Status { get; set; } = AccountStatusEnum.enabled;

        [Required]
        public UsageEnum Usage { get; set; }

        [Required]
        public AmountDto InitialBalance { get; set; }

        [Required]
        public string Msisdn { get; set; }
    }
}