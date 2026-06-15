using MediatR;
using TodoPlatform.Domain.Common;

namespace TodoPlatform.Domain.Events;

public sealed record TodoDeletedEvent(
    Guid TodoId,
    Guid UserId) : IDomainEvent, INotification
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
