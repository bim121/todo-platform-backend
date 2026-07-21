using TodoPlatform.Domain.Common;

namespace TodoPlatform.Application.Interfaces;

/// <summary>
/// Stages domain events into the outbox table within the current unit of work.
/// A background outbox processor publishes pending rows to the message bus.
/// </summary>
public interface IOutboxStore
{
    void Stage(IEnumerable<IDomainEvent> domainEvents);
}
