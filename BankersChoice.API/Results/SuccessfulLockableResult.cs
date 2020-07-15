namespace BankersChoice.API.Results
{
    public class SuccessfulLockableResult<T> : LockableResult<T>
    {
        public SuccessfulLockableResult(T value)
        {
            Value = value;
        }

        public T Value { get; set; }
    }
}