using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using TodoPlatform.Application.Todos.EventHandlers;
using TodoPlatform.Domain.Events;

namespace TodoPlatform.Application.Tests.Todos.EventHandlers;

public sealed class TodoCreatedEventHandlerTests
{
    [Fact]
    public async Task AuditHandler_LogsTodoCreated()
    {
        var logger = new Mock<ILogger<TodoCreatedAuditHandler>>();
        var handler = new TodoCreatedAuditHandler(logger.Object);
        var evt = new TodoCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Audit me");

        await handler.Handle(evt, CancellationToken.None);

        logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Todo created")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
