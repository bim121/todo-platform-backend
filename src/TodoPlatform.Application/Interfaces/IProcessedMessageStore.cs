namespace TodoPlatform.Application.Interfaces;

/// <summary>
/// Consumer-side idempotency: each MassTransit <c>MessageId</c> is processed at most once.
/// </summary>
public interface IProcessedMessageStore
{
    /// <summary>
    /// Tries to record <paramref name="messageId"/> as processed.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if this is the first acquisition (run the side effect);
    /// <see langword="false"/> if the message was already processed (skip).
    /// </returns>
    Task<bool> TryAcquireAsync(Guid messageId, CancellationToken cancellationToken = default);
}
