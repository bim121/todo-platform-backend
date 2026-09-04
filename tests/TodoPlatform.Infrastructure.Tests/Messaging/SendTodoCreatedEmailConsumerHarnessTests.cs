using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TodoPlatform.Application.IntegrationEvents;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Domain.Entities;
using TodoPlatform.Infrastructure.Messaging.Consumers;

namespace TodoPlatform.Infrastructure.Tests.Messaging;

public sealed class SendTodoCreatedEmailConsumerHarnessTests
{
    [Fact]
    public async Task Harness_ConsumerReceivesTodoCreatedIntegrationEvent()
    {
        var userId = Guid.NewGuid();
        var processed = new Mock<IProcessedMessageStore>();
        processed.Setup(s => s.TryAcquireAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var users = new Mock<IUserRepository>();
        users.Setup(u => u.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(User.Register("harness@example.com", "hash", "Harness"));

        var email = new Mock<IEmailSender>();
        email.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await using var provider = new ServiceCollection()
            .AddSingleton(processed.Object)
            .AddSingleton(users.Object)
            .AddSingleton(email.Object)
            .AddLogging()
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddConsumer<SendTodoCreatedEmailConsumer>();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            var message = new TodoCreatedIntegrationEvent(
                Guid.NewGuid(),
                userId,
                Guid.NewGuid(),
                "Harness todo",
                false,
                DateTimeOffset.UtcNow);

            await harness.Bus.Publish(message);

            Assert.True(await harness.Consumed.Any<TodoCreatedIntegrationEvent>());
            Assert.True(await harness.GetConsumerHarness<SendTodoCreatedEmailConsumer>()
                .Consumed.Any<TodoCreatedIntegrationEvent>());

            email.Verify(
                e => e.SendAsync(
                    "harness@example.com",
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            await harness.Stop();
        }
    }
}
