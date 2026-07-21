using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TodoPlatform.Application.IntegrationEvents;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Infrastructure.Messaging.Consumers;

namespace TodoPlatform.Infrastructure.Tests.Messaging;

public sealed class SendTodoCreatedEmailConsumerTests
{
    [Fact]
    public async Task Consume_FirstDelivery_AcquiresAndLogsSideEffect()
    {
        var store = new Mock<IProcessedMessageStore>();
        store.Setup(s => s.TryAcquireAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var consumer = new SendTodoCreatedEmailConsumer(store.Object, NullLogger<SendTodoCreatedEmailConsumer>.Instance);
        var messageId = Guid.NewGuid();
        var context = CreateContext(messageId, new TodoCreatedIntegrationEvent(
            Guid.NewGuid(), Guid.NewGuid(), "Title", DateTimeOffset.UtcNow));

        await consumer.Consume(context.Object);

        store.Verify(s => s.TryAcquireAsync(messageId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Consume_DuplicateDelivery_SkipsSideEffect()
    {
        var store = new Mock<IProcessedMessageStore>();
        store.Setup(s => s.TryAcquireAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var consumer = new SendTodoCreatedEmailConsumer(store.Object, NullLogger<SendTodoCreatedEmailConsumer>.Instance);
        var context = CreateContext(Guid.NewGuid(), new TodoCreatedIntegrationEvent(
            Guid.NewGuid(), Guid.NewGuid(), "Title", DateTimeOffset.UtcNow));

        await consumer.Consume(context.Object);

        store.Verify(s => s.TryAcquireAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Mock<ConsumeContext<TodoCreatedIntegrationEvent>> CreateContext(
        Guid messageId,
        TodoCreatedIntegrationEvent message)
    {
        var context = new Mock<ConsumeContext<TodoCreatedIntegrationEvent>>();
        context.SetupGet(c => c.Message).Returns(message);
        context.SetupGet(c => c.MessageId).Returns(messageId);
        context.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);
        return context;
    }
}
