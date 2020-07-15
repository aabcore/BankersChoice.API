using System;
using BankersChoice.API.Models.Entities.Account;

namespace BankersChoice.API.Models.ApiDtos.Account
{
    public class LockOutDto
    {
        public bool IsLocked { get; set; }
        public Guid LockedBy { get; set; }

        public static LockOutDto EntityToOutDto(LockEntity lockEntity)
        {
            return lockEntity != null
                ? new LockOutDto()
                {
                    LockedBy = lockEntity.LockedBy,
                    IsLocked = lockEntity.IsLocked
                }
                : null;
        }
    }
}