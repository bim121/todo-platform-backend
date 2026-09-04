using System.Text.Json;
using TodoPlatform.Application.IntegrationEvents;
using TodoPlatform.Domain.Events;
using TodoPlatform.Infrastructure.Messaging;

namespace TodoPlatform.Infrastructure.Tests.Messaging;

public sealed class DomainEventToIntegrationEventMapperTests
{
    private readonly DomainEventToIntegrationEventMapper _sut = new();

    [Fact]
    public void Map_TodoCreatedEvent_ReturnsEnvelopeWithVersionAndData()
    {
        var todoId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var domainEvent = new TodoCreatedEvent(todoId, userId, tenantId, "Buy milk");

        var envelope = _sut.Map(domainEvent);

        Assert.NotNull(envelope);
        Assert.Equal(TodoCreatedIntegrationEvent.EventTypeName, envelope.Type);
        Assert.Equal(IntegrationEventEnvelope.CurrentVersion, envelope.Version);
        Assert.Equal(domainEvent.OccurredOn, envelope.OccurredOn);

        var data = Assert.IsType<TodoCreatedIntegrationEvent>(envelope.Data);
        Assert.Equal(todoId, data.TodoId);
        Assert.Equal(userId, data.UserId);
        Assert.Equal(tenantId, data.TenantId);
        Assert.Equal("Buy milk", data.Title);
        Assert.False(data.Completed);
    }

    [Fact]
    public void Map_TodoUpdatedEvent_ReturnsEnvelope()
    {
        var todoId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var domainEvent = new TodoUpdatedEvent(todoId, userId, tenantId, "Renamed", true);

        var envelope = _sut.Map(domainEvent);

        Assert.NotNull(envelope);
        Assert.Equal(TodoUpdatedIntegrationEvent.EventTypeName, envelope.Type);
        var data = Assert.IsType<TodoUpdatedIntegrationEvent>(envelope.Data);
        Assert.Equal(todoId, data.TodoId);
        Assert.Equal("Renamed", data.Title);
        Assert.True(data.Completed);
    }

    [Fact]
    public void Map_TodoDeletedEvent_ReturnsEnvelope()
    {
        var todoId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var domainEvent = new TodoDeletedEvent(todoId, userId, tenantId, "Bye", false);

        var envelope = _sut.Map(domainEvent);

        Assert.NotNull(envelope);
        Assert.Equal(TodoDeletedIntegrationEvent.EventTypeName, envelope.Type);
        var data = Assert.IsType<TodoDeletedIntegrationEvent>(envelope.Data);
        Assert.Equal(todoId, data.TodoId);
        Assert.Equal("Bye", data.Title);
    }

    [Fact]
    public void Map_TodoCompletedEvent_ReturnsEnvelope()
    {
        var todoId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var domainEvent = new TodoCompletedEvent(todoId, userId, Guid.NewGuid());

        var envelope = _sut.Map(domainEvent);

        Assert.NotNull(envelope);
        Assert.Equal(TodoCompletedIntegrationEvent.EventTypeName, envelope.Type);
        var data = Assert.IsType<TodoCompletedIntegrationEvent>(envelope.Data);
        Assert.Equal(todoId, data.TodoId);
        Assert.Equal(userId, data.UserId);
    }

    [Fact]
    public void Map_TenantMigrationAppliedEvent_ReturnsEnvelope()
    {
        var tenantId = Guid.NewGuid();
        var domainEvent = new TenantMigrationAppliedEvent(tenantId, "V012-beta-feature", "admin@test");

        var envelope = _sut.Map(domainEvent);

        Assert.NotNull(envelope);
        Assert.Equal(TenantMigrationAppliedIntegrationEvent.EventTypeName, envelope.Type);
        var data = Assert.IsType<TenantMigrationAppliedIntegrationEvent>(envelope.Data);
        Assert.Equal(tenantId, data.TenantId);
        Assert.Equal("V012-beta-feature", data.Version);
        Assert.Equal("admin@test", data.AppliedBy);
    }

    [Fact]
    public void Envelope_SerializesToTypeVersionDataShape()
    {
        var domainEvent = new TodoCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Title");
        var envelope = _sut.Map(domainEvent)!;

        var json = JsonSerializer.Serialize(envelope, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.Contains("\"type\":\"TodoCreatedIntegrationEvent\"", json, StringComparison.Ordinal);
        Assert.Contains("\"version\":1", json, StringComparison.Ordinal);
        Assert.Contains("\"data\":", json, StringComparison.Ordinal);
    }
}
