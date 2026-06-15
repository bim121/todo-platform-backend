using TodoPlatform.Domain.Common;

namespace TodoPlatform.Domain.Events;

public sealed record TodoDeletedEvent(
    Guid TodoId,
    Guid UserId) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
