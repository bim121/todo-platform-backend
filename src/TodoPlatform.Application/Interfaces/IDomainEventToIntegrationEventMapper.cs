using TodoPlatform.Application.IntegrationEvents;
using TodoPlatform.Domain.Common;

namespace TodoPlatform.Application.Interfaces;

/// <summary>
/// Maps in-process domain events to cross-boundary integration events for the outbox.
/// </summary>
public interface IDomainEventToIntegrationEventMapper
{
    /// <summary>
    /// Returns an envelope when the domain event should be published externally; otherwise null.
    /// </summary>
    IntegrationEventEnvelope? Map(IDomainEvent domainEvent);
}
