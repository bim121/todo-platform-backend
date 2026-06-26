namespace TodoPlatform.Infrastructure.Persistence;

public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Type { get; init; } = string.Empty;

    public string Payload { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ProcessedAt { get; set; }
}
