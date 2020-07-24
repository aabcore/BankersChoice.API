using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BankersChoice.API.Models
{
    public class BadRequestOutDto
    {
        public BadRequestOutDto(string message)
        {
            Message = message;
        }

        public string Message { get; set; }

        public static BadRequestOutDto AccountNotLocked => new BadRequestOutDto("Account must be locked to update");
        public static BadRequestOutDto AccountLockedByAnotherUser => new BadRequestOutDto("Account is locked by another user");
        public static BadRequestOutDto AccountNotFound => new BadRequestOutDto("Given account id does not exist.");
        public static BadRequestOutDto AccountNotEnabled => new BadRequestOutDto("Account is not enabled, and the requested action is only allowed on enabled accounts.");
        public static BadRequestOutDto WrongCurrencyType => new BadRequestOutDto("Attempted to use the wrong Currency type");
    }
}
