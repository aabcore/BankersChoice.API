using System;

namespace BankersChoice.API.Results
{
    public class FailedLockableResult<T> : LockableResult<T>
    {
        public FailedLockableResult(Exception e)
        {
            Error = e;
        }

        public Exception Error { get; set; }
    }
}