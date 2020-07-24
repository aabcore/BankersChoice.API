using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BankersChoice.API.Models.ApiDtos
{
    public class WizardingCurrencyInDto : AmountInDto
    {
        [Required]
        public long? Galleons { get; set; }

        [Required]
        public long? Sickles { get; set; }

        [Required]
        public long? Knuts { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!Galleons.HasValue)
            {
                yield return new ValidationResult("The Galleons field is required", new []{nameof(Galleons)});
            }
            if (!Sickles.HasValue)
            {
                yield return new ValidationResult("The Sickles field is required", new[] { nameof(Sickles) });
            }
            if (!Knuts.HasValue)
            {
                yield return new ValidationResult("The Knuts field is required", new[] { nameof(Knuts) });
            }
        }
    }
}