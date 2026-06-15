using TodoPlatform.Domain.Common;

namespace TodoPlatform.Application.Common;

public interface IDomainEventDispatcher
{
    Task DispatchEventsAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default);
}
