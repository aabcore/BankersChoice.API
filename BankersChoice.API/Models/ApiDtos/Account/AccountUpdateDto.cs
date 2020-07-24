using System.ComponentModel.DataAnnotations;
using BankersChoice.API.Models.Entities.Account;

namespace BankersChoice.API.Models.ApiDtos.Account
{
    public class AccountUpdateDto
    {
        public string Name { get; set; }
        public AccountStatusEnum? Status { get; set; }
        public string Msisdn { get; set; }

        [Required]
        public string LockSecret { get; set; }
    }
}