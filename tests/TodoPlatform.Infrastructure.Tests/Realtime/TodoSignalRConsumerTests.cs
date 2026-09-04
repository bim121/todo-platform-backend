using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TodoPlatform.Application.IntegrationEvents;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Realtime;
using TodoPlatform.Infrastructure.Realtime;

namespace TodoPlatform.Infrastructure.Tests.Realtime;

public sealed class TodoSignalRConsumerTests
{
    [Fact]
    public async Task CreatedConsumer_NotifiesGroupScopedNotifier()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var todoId = Guid.NewGuid();
        TodoRealtimeMessage? pushed = null;
        Guid? pushedTenant = null;
        Guid? pushedUser = null;

        var notifier = new Mock<ITodoRealtimeNotifier>();
        notifier
            .Setup(n => n.NotifyCreatedAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<TodoRealtimeMessage>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, TodoRealtimeMessage, CancellationToken>((t, u, m, _) =>
            {
                pushedTenant = t;
                pushedUser = u;
                pushed = m;
            })
            .Returns(Task.CompletedTask);

        var consumer = new TodoCreatedSignalRConsumer(
            notifier.Object,
            NullLogger<TodoCreatedSignalRConsumer>.Instance);

        var context = new Mock<ConsumeContext<TodoCreatedIntegrationEvent>>();
        context.SetupGet(c => c.Message).Returns(new TodoCreatedIntegrationEvent(
            todoId, userId, tenantId, "Live", false, DateTimeOffset.UtcNow));
        context.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(context.Object);

        Assert.Equal(tenantId, pushedTenant);
        Assert.Equal(userId, pushedUser);
        Assert.NotNull(pushed);
        Assert.Equal(todoId, pushed.Id);
        Assert.Equal("Live", pushed.Title);
        Assert.False(pushed.Completed);
    }

    [Fact]
    public async Task UpdatedConsumer_NotifiesGroupScopedNotifier()
    {
        var notifier = new Mock<ITodoRealtimeNotifier>();
        notifier
            .Setup(n => n.NotifyUpdatedAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<TodoRealtimeMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var consumer = new TodoUpdatedSignalRConsumer(
            notifier.Object,
            NullLogger<TodoUpdatedSignalRConsumer>.Instance);

        var context = new Mock<ConsumeContext<TodoUpdatedIntegrationEvent>>();
        context.SetupGet(c => c.Message).Returns(new TodoUpdatedIntegrationEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Updated", true, DateTimeOffset.UtcNow));
        context.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(context.Object);

        notifier.Verify(
            n => n.NotifyUpdatedAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<TodoRealtimeMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeletedConsumer_NotifiesGroupScopedNotifier()
    {
        var notifier = new Mock<ITodoRealtimeNotifier>();
        notifier
            .Setup(n => n.NotifyDeletedAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<TodoRealtimeMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var consumer = new TodoDeletedSignalRConsumer(
            notifier.Object,
            NullLogger<TodoDeletedSignalRConsumer>.Instance);

        var context = new Mock<ConsumeContext<TodoDeletedIntegrationEvent>>();
        context.SetupGet(c => c.Message).Returns(new TodoDeletedIntegrationEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Deleted", false, DateTimeOffset.UtcNow));
        context.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(context.Object);

        notifier.Verify(
            n => n.NotifyDeletedAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<TodoRealtimeMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
