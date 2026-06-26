using MediatR;
using TodoPlatform.Domain.Common;

namespace TodoPlatform.Domain.Events;

public sealed record UserRegisteredEvent(
    Guid UserId,
    string Email,
    string KeycloakSub) : IDomainEvent, INotification
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
