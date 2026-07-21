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
        var domainEvent = new TodoCreatedEvent(todoId, userId, "Buy milk");

        var envelope = _sut.Map(domainEvent);

        Assert.NotNull(envelope);
        Assert.Equal(TodoCreatedIntegrationEvent.EventTypeName, envelope.Type);
        Assert.Equal(IntegrationEventEnvelope.CurrentVersion, envelope.Version);
        Assert.Equal(domainEvent.OccurredOn, envelope.OccurredOn);

        var data = Assert.IsType<TodoCreatedIntegrationEvent>(envelope.Data);
        Assert.Equal(todoId, data.TodoId);
        Assert.Equal(userId, data.UserId);
        Assert.Equal("Buy milk", data.Title);
    }

    [Fact]
    public void Map_UnknownDomainEvent_ReturnsNull()
    {
        var domainEvent = new TodoCompletedEvent(Guid.NewGuid(), Guid.NewGuid());

        Assert.Null(_sut.Map(domainEvent));
    }

    [Fact]
    public void Envelope_SerializesToTypeVersionDataShape()
    {
        var domainEvent = new TodoCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), "Title");
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
