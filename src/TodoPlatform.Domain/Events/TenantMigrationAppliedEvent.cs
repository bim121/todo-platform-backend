using MediatR;
using TodoPlatform.Domain.Common;

namespace TodoPlatform.Domain.Events;

public sealed record TenantMigrationAppliedEvent(
    Guid TenantId,
    string Version,
    string AppliedBy) : IDomainEvent, INotification
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
