using System.ComponentModel.DataAnnotations;
using System.Linq;
using BankersChoice.API.Models.Entities.Account;

namespace BankersChoice.API.Models.ApiDtos.Account
{
    public class AccountReferenceOutDto
    {
        public string Pan { get; set; }
        public string MaskedPan { get; set; }
        public RoutingNumbersOutDto RoutingNumbers { get; set; }
        public CurrencyEnum Currency { get; set; }
        public string Msisdn { get; set; }

        public static T EntityToOutDto<T>(AccountReferenceEntity accountReferenceEntity) where T: AccountReferenceOutDto, new()
        {
            return new T()
            {
                Pan = accountReferenceEntity.Pan,
                MaskedPan = accountReferenceEntity.Pan.Substring(0, 2) +
                            string.Join("", Enumerable.Repeat("x", accountReferenceEntity.Pan.Length - 4)) + 
                            accountReferenceEntity.Pan.Substring(accountReferenceEntity.Pan.Length - 2, 2),
                Msisdn = accountReferenceEntity.Msisdn,
                Currency = accountReferenceEntity.Currency,
                RoutingNumbers = RoutingNumbersOutDto.EntityToOutDto(accountReferenceEntity.RoutingNumbers)
            };
        }
    }

    public class AccountReferenceInDto
    {
        [Required]
        public string Pan { get; set; }
        [Required]
        public RoutingNumbersOutDto RoutingNumbers { get; set; }
        [Required]
        public CurrencyEnum Currency { get; set; }
        public string Msisdn { get; set; }
    }
}