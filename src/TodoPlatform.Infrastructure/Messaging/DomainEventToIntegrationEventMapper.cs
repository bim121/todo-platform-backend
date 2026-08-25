using TodoPlatform.Application.IntegrationEvents;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Domain.Common;
using TodoPlatform.Domain.Events;

namespace TodoPlatform.Infrastructure.Messaging;

public sealed class DomainEventToIntegrationEventMapper : IDomainEventToIntegrationEventMapper
{
    public IntegrationEventEnvelope? Map(IDomainEvent domainEvent) =>
        domainEvent switch
        {
            TodoCreatedEvent created => new IntegrationEventEnvelope(
                Type: TodoCreatedIntegrationEvent.EventTypeName,
                Version: IntegrationEventEnvelope.CurrentVersion,
                Data: new TodoCreatedIntegrationEvent(
                    created.TodoId,
                    created.UserId,
                    created.Title,
                    created.OccurredOn),
                OccurredOn: created.OccurredOn),
            TodoCompletedEvent completed => new IntegrationEventEnvelope(
                Type: TodoCompletedIntegrationEvent.EventTypeName,
                Version: IntegrationEventEnvelope.CurrentVersion,
                Data: new TodoCompletedIntegrationEvent(
                    completed.TodoId,
                    completed.UserId,
                    completed.OccurredOn),
                OccurredOn: completed.OccurredOn),
            TenantMigrationAppliedEvent applied => new IntegrationEventEnvelope(
                Type: TenantMigrationAppliedIntegrationEvent.EventTypeName,
                Version: IntegrationEventEnvelope.CurrentVersion,
                Data: new TenantMigrationAppliedIntegrationEvent(
                    applied.TenantId,
                    applied.Version,
                    applied.AppliedBy,
                    applied.OccurredOn),
                OccurredOn: applied.OccurredOn),
            _ => null
        };
}
