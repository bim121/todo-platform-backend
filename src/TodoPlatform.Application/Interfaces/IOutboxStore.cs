using TodoPlatform.Domain.Common;

namespace TodoPlatform.Application.Interfaces;

/// <summary>
/// Stages domain events into the outbox table within the current unit of work (publisher in B-07).
/// </summary>
public interface IOutboxStore
{
    void Stage(IEnumerable<IDomainEvent> domainEvents);
}
