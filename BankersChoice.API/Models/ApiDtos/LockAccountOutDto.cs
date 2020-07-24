using System;

namespace BankersChoice.API.Models.ApiDtos
{
    public class LockAccountOutDto
    {
        public Guid ResourceId { get; set; }
        public Guid UserId { get; set; }
        public bool GotLock { get; set; }
        public string LockSecret { get; set; }
    }
}