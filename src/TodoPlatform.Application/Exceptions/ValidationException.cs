namespace TodoPlatform.Application.Exceptions;

public sealed class ValidationException : Exception
{
    public ValidationException(IDictionary<string, string[]> errors)
        : this("One or more validation errors occurred.", errors)
    {
    }

    public ValidationException(string message, IDictionary<string, string[]> errors)
        : base(message)
    {
        Errors = errors;
    }

    public IDictionary<string, string[]> Errors { get; }

    public static ValidationException ForField(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
