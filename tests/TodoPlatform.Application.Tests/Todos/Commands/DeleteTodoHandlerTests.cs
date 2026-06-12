using Moq;
using TodoPlatform.Application.Exceptions;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Todos.Commands.DeleteTodo;

namespace TodoPlatform.Application.Tests.Todos.Commands;

public sealed class DeleteTodoHandlerTests
{
    [Fact]
    public async Task Handle_ExistingTodo_DeletesSuccessfully()
    {
        var id = Guid.NewGuid();
        var repository = new Mock<ITodoRepository>();
        repository
            .Setup(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new DeleteTodoHandler(repository.Object);
        await handler.Handle(new DeleteTodoCommand(id), CancellationToken.None);

        repository.Verify(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MissingTodo_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        var repository = new Mock<ITodoRepository>();
        repository
            .Setup(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new DeleteTodoHandler(repository.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteTodoCommand(id), CancellationToken.None));
    }
}
