using System;

namespace BankersChoice.API.Results
{
    public class FailedTypedResult<T> : TypedResult<T>
    {
        public FailedTypedResult(Exception e)
        {
            Error = e;
        }

        public Exception Error { get; set; }
    }
}