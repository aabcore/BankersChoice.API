using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BankersChoice.API.Results
{
    public abstract class Result
    {
    }

    public class SuccessResult: Result{}

    public class FailedResult : Result
    {
        public Exception Exception { get; set; }

        public FailedResult(Exception exception)
        {
            Exception = exception;
        }
    }
}
