using Microsoft.EntityFrameworkCore;
using TodoPlatform.Application.IntegrationEvents;
using TodoPlatform.Infrastructure.Messaging;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Infrastructure.Tests.Messaging;

public sealed class IntegrationEventPayloadDeserializerTests
{
    [Fact]
    public void Deserialize_TodoCreatedEnvelope_ReturnsTypedEvent()
    {
        var todoId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var occurredOn = DateTimeOffset.Parse("2026-07-21T12:00:00Z");
        var payload =
            $$"""
            {"type":"TodoCreatedIntegrationEvent","version":1,"data":{"todoId":"{{todoId}}","userId":"{{userId}}","title":"Hello","occurredOn":"{{occurredOn:O}}"},"occurredOn":"{{occurredOn:O}}"}
            """;

        var result = IntegrationEventPayloadDeserializer.Deserialize(
            TodoCreatedIntegrationEvent.EventTypeName,
            payload);

        var created = Assert.IsType<TodoCreatedIntegrationEvent>(result);
        Assert.Equal(todoId, created.TodoId);
        Assert.Equal(userId, created.UserId);
        Assert.Equal("Hello", created.Title);
    }

    [Fact]
    public void Deserialize_UnknownType_ReturnsNull()
    {
        var payload = """{"type":"Other","version":1,"data":{},"occurredOn":"2026-07-21T12:00:00Z"}""";

        Assert.Null(IntegrationEventPayloadDeserializer.Deserialize("Other", payload));
    }
}

public sealed class EfProcessedMessageStoreTests
{
    [Fact]
    public async Task TryAcquireAsync_FirstCall_ReturnsTrue_SecondCall_ReturnsFalse()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        var store = new EfProcessedMessageStore(db);
        var messageId = Guid.NewGuid();

        Assert.True(await store.TryAcquireAsync(messageId));
        Assert.False(await store.TryAcquireAsync(messageId));
        Assert.Single(await db.ProcessedMessages.ToListAsync());
    }
}
