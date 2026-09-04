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
                    created.TenantId,
                    created.Title,
                    Completed: false,
                    created.OccurredOn),
                OccurredOn: created.OccurredOn),
            TodoUpdatedEvent updated => new IntegrationEventEnvelope(
                Type: TodoUpdatedIntegrationEvent.EventTypeName,
                Version: IntegrationEventEnvelope.CurrentVersion,
                Data: new TodoUpdatedIntegrationEvent(
                    updated.TodoId,
                    updated.UserId,
                    updated.TenantId,
                    updated.Title,
                    updated.Completed,
                    updated.OccurredOn),
                OccurredOn: updated.OccurredOn),
            TodoDeletedEvent deleted => new IntegrationEventEnvelope(
                Type: TodoDeletedIntegrationEvent.EventTypeName,
                Version: IntegrationEventEnvelope.CurrentVersion,
                Data: new TodoDeletedIntegrationEvent(
                    deleted.TodoId,
                    deleted.UserId,
                    deleted.TenantId,
                    deleted.Title,
                    deleted.Completed,
                    deleted.OccurredOn),
                OccurredOn: deleted.OccurredOn),
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
