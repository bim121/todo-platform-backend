namespace TodoPlatform.Application.Exceptions;

/// <summary>RFC 7807 conflict (e.g. migration target not next pending).</summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message)
    {
    }
}
