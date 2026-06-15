using MediatR;
using TodoPlatform.Domain.Common;

namespace TodoPlatform.Application.Common;

public sealed class DomainEventDispatcher(IMediator mediator) : IDomainEventDispatcher
{
    public async Task DispatchEventsAsync(
        IEnumerable<IDomainEvent> events,
        CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in events)
            await mediator.Publish(domainEvent, cancellationToken);
    }
}
