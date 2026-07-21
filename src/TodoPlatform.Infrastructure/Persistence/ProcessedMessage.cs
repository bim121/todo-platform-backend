namespace TodoPlatform.Infrastructure.Persistence;

/// <summary>
/// Tracks MassTransit message IDs already handled by consumers (at-least-once delivery).
/// </summary>
public sealed class ProcessedMessage
{
    public Guid MessageId { get; init; }

    public DateTimeOffset ProcessedAt { get; init; } = DateTimeOffset.UtcNow;
}
