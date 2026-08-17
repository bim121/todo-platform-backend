using MediatR;
using Moq;
using TodoPlatform.Application.Common;
using TodoPlatform.Domain.Events;

namespace TodoPlatform.Application.Tests.Domain;

public sealed class DomainEventDispatcherTests
{
    [Fact]
    public async Task DispatchEventsAsync_PublishesEachEventViaMediator()
    {
        var mediator = new Mock<IMediator>();
        var dispatcher = new DomainEventDispatcher(mediator.Object);
        var created = new TodoCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Test");
        var completed = new TodoCompletedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        await dispatcher.DispatchEventsAsync([created, completed]);

        mediator.Verify(
            m => m.Publish(It.Is<object>(n => n == created), It.IsAny<CancellationToken>()),
            Times.Once);
        mediator.Verify(
            m => m.Publish(It.Is<object>(n => n == completed), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
