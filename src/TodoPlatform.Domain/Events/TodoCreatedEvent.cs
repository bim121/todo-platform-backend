using TodoPlatform.Domain.Common;

namespace TodoPlatform.Domain.Events;

public sealed record TodoCreatedEvent(
    Guid TodoId,
    Guid UserId,
    string Title) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
