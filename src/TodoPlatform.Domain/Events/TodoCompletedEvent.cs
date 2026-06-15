using TodoPlatform.Domain.Common;

namespace TodoPlatform.Domain.Events;

public sealed record TodoCompletedEvent(
    Guid TodoId,
    Guid UserId) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
