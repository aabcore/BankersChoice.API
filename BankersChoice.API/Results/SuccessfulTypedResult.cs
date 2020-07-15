namespace BankersChoice.API.Results
{
    public class SuccessfulTypedResult<T> : TypedResult<T>
    {
        public SuccessfulTypedResult(T value)
        {
            Value = value;
        }

        public T Value { get; set; }
    }
}