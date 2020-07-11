namespace BankersChoice.API.Models.Account
{
    public class Lock
    {
        public bool IsLocked { get; set; }
        public string LockedBy { get; set; }
    }
}