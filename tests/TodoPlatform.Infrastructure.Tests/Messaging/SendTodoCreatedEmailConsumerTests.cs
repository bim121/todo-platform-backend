using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TodoPlatform.Application.IntegrationEvents;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Domain.Entities;
using TodoPlatform.Infrastructure.Messaging.Consumers;

namespace TodoPlatform.Infrastructure.Tests.Messaging;

public sealed class SendTodoCreatedEmailConsumerTests
{
    [Fact]
    public async Task Consume_FirstDelivery_SendsEmailAndLogsWithUserEmail()
    {
        var userId = Guid.NewGuid();
        var store = new Mock<IProcessedMessageStore>();
        store.Setup(s => s.TryAcquireAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var users = new Mock<IUserRepository>();
        users.Setup(u => u.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(User.Register("user@example.com", "hash", "User"));

        string? sentTo = null;
        var email = new Mock<IEmailSender>();
        email.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((to, _, _, _) => sentTo = to)
            .Returns(Task.CompletedTask);

        var consumer = new SendTodoCreatedEmailConsumer(
            store.Object,
            users.Object,
            email.Object,
            NullLogger<SendTodoCreatedEmailConsumer>.Instance);

        var messageId = Guid.NewGuid();
        var context = CreateContext(messageId, new TodoCreatedIntegrationEvent(
            Guid.NewGuid(), userId, "Title", DateTimeOffset.UtcNow));

        await consumer.Consume(context.Object);

        Assert.Equal("user@example.com", sentTo);
        email.Verify(
            e => e.SendAsync("user@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Consume_DuplicateDelivery_SkipsSideEffect()
    {
        var store = new Mock<IProcessedMessageStore>();
        store.Setup(s => s.TryAcquireAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var email = new Mock<IEmailSender>();
        var consumer = new SendTodoCreatedEmailConsumer(
            store.Object,
            Mock.Of<IUserRepository>(),
            email.Object,
            NullLogger<SendTodoCreatedEmailConsumer>.Instance);

        var context = CreateContext(Guid.NewGuid(), new TodoCreatedIntegrationEvent(
            Guid.NewGuid(), Guid.NewGuid(), "Title", DateTimeOffset.UtcNow));

        await consumer.Consume(context.Object);

        email.Verify(
            e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
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
