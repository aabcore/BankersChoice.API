using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BankersChoice.API.Models.ApiDtos
{
    public class BluCoinCurrencyInDto : AmountInDto
    {
        [Required] public long? Tuns { get; set; }
        [Required] public long? Scraposts { get; set; }
        [Required] public long? Katts { get; set; }
        [Required] public long? Kibels { get; set; }


        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!Tuns.HasValue)
            {
                yield return new ValidationResult("The Tuns field is required", new[] {nameof(Tuns)});
            }

            if (!Scraposts.HasValue)
            {
                yield return new ValidationResult("The Scraposts field is required", new[] {nameof(Scraposts)});
            }

            if (!Katts.HasValue)
            {
                yield return new ValidationResult("The Katts field is required", new[] {nameof(Katts)});
            }

            if (!Kibels.HasValue)
            {
                yield return new ValidationResult("The Kibels field is required", new[] {nameof(Kibels)});
            }
        }
    }
}