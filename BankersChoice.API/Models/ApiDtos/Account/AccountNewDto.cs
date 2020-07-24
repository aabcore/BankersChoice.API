using System.ComponentModel.DataAnnotations;
using BankersChoice.API.Models.Entities.Account;

namespace BankersChoice.API.Models.ApiDtos.Account
{
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
        public AmountInDto InitialBalance { get; set; }

        [Required]
        public string Msisdn { get; set; }

        public AmountOutDto AuthorizedLimit { get; set; }
    }
}