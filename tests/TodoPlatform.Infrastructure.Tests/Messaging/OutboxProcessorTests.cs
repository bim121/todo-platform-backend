using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TodoPlatform.Application.IntegrationEvents;
using TodoPlatform.Infrastructure.Messaging;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Infrastructure.Tests.Messaging;

public sealed class OutboxProcessorTests
{
    [Fact]
    public async Task PublishPendingAsync_PublishesIntegrationEventAndMarksProcessed()
    {
        var databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var outboxId = Guid.NewGuid();
        var todoId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var occurredOn = DateTimeOffset.UtcNow;

        await using (var seed = new AppDbContext(options))
        {
            seed.OutboxMessages.Add(new OutboxMessage
            {
                Id = outboxId,
                Type = TodoCreatedIntegrationEvent.EventTypeName,
                Payload =
                    $$"""
                    {"type":"TodoCreatedIntegrationEvent","version":1,"data":{"todoId":"{{todoId}}","userId":"{{userId}}","title":"Async","occurredOn":"{{occurredOn:O}}"},"occurredOn":"{{occurredOn:O}}"}
                    """,
                CreatedAt = occurredOn
            });
            await seed.SaveChangesAsync();
        }

        object? published = null;
        var publishEndpoint = new Mock<IPublishEndpoint>();
        publishEndpoint
            .Setup(p => p.Publish(
                It.IsAny<object>(),
                It.IsAny<Type>(),
                It.IsAny<IPipe<PublishContext>>(),
                It.IsAny<CancellationToken>()))
            .Callback<object, Type, IPipe<PublishContext>, CancellationToken>((msg, _, _, _) => published = msg)
            .Returns(Task.CompletedTask);

        await using var provider = new ServiceCollection()
            .AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(databaseName))
            .AddSingleton(publishEndpoint.Object)
            .BuildServiceProvider();

        var processor = new OutboxProcessor(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<OutboxProcessor>.Instance);

        var count = await processor.PublishPendingAsync();

        Assert.Equal(1, count);
        var created = Assert.IsType<TodoCreatedIntegrationEvent>(published);
        Assert.Equal(todoId, created.TodoId);
        Assert.Equal("Async", created.Title);

        await using var assertDb = new AppDbContext(options);
        var row = Assert.Single(await assertDb.OutboxMessages.ToListAsync());
        Assert.NotNull(row.ProcessedAt);
    }

    [Fact]
    public async Task PublishPendingAsync_WhenPublishFails_LeavesUnprocessed()
    {
        var databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        await using (var seed = new AppDbContext(options))
        {
            seed.OutboxMessages.Add(new OutboxMessage
            {
                Type = TodoCreatedIntegrationEvent.EventTypeName,
                Payload =
                    """{"type":"TodoCreatedIntegrationEvent","version":1,"data":{"todoId":"00000000-0000-0000-0000-000000000001","userId":"00000000-0000-0000-0000-000000000002","title":"X","occurredOn":"2026-07-21T12:00:00Z"},"occurredOn":"2026-07-21T12:00:00Z"}""",
                CreatedAt = DateTimeOffset.UtcNow
            });
            await seed.SaveChangesAsync();
        }

        var publishEndpoint = new Mock<IPublishEndpoint>();
        publishEndpoint
            .Setup(p => p.Publish(
                It.IsAny<object>(),
                It.IsAny<Type>(),
                It.IsAny<IPipe<PublishContext>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("broker down"));

        await using var provider = new ServiceCollection()
            .AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(databaseName))
            .AddSingleton(publishEndpoint.Object)
            .BuildServiceProvider();

        var processor = new OutboxProcessor(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<OutboxProcessor>.Instance);

        var count = await processor.PublishPendingAsync();

        Assert.Equal(0, count);
        await using var assertDb = new AppDbContext(options);
        Assert.Null((await assertDb.OutboxMessages.SingleAsync()).ProcessedAt);
    }
}
