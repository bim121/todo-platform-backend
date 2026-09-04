using MediatR;
using TodoPlatform.Domain.Common;

namespace TodoPlatform.Domain.Events;

public sealed record TodoUpdatedEvent(
    Guid TodoId,
    Guid UserId,
    Guid TenantId,
    string Title,
    bool Completed) : IDomainEvent, INotification
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
