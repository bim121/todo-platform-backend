using TodoPlatform.Domain.Common;

namespace TodoPlatform.Application.Common;

public static class DomainEventEntityExtensions
{
    public static async Task DispatchAndClearDomainEventsAsync(
        this Entity entity,
        IDomainEventDispatcher dispatcher,
        CancellationToken cancellationToken = default)
    {
        if (entity.DomainEvents.Count == 0)
            return;

        var events = entity.DomainEvents.ToList();
        entity.ClearDomainEvents();
        await dispatcher.DispatchEventsAsync(events, cancellationToken);
    }
}
