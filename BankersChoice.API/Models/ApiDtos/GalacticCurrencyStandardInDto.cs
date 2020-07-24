using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BankersChoice.API.Models.ApiDtos
{
    public class GalacticCurrencyStandardInDto : AmountInDto
    {
        [Required]
        public long? Amount { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!Amount.HasValue)
            {
                yield return new ValidationResult("The Amount field is required", new[] { nameof(Amount) });
            }
        }
    }
}