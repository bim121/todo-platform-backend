using Moq;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Exceptions;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Todos.Commands.UpdateTodo;
using TodoPlatform.Domain.Entities;

namespace TodoPlatform.Application.Tests.Todos.Commands;

public sealed class UpdateTodoHandlerTests
{
    [Fact]
    public async Task Handle_ExistingTodo_UpdatesAndReturnsDto()
    {
        var todo = Todo.Create("Old title", Guid.NewGuid());
        Todo? updated = null;

        var repository = new Mock<ITodoRepository>();
        repository
            .Setup(r => r.GetByIdAsync(todo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(todo);
        repository
            .Setup(r => r.UpdateAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()))
            .Callback<Todo, CancellationToken>((entity, _) => updated = entity)
            .Returns(Task.CompletedTask);

        var handler = new UpdateTodoHandler(repository.Object);
        var result = await handler.Handle(
            new UpdateTodoCommand(todo.Id, new UpdateTodoRequest(Title: "New title", Completed: true)),
            CancellationToken.None);

        Assert.Equal("New title", result.Title);
        Assert.True(result.Completed);
        Assert.Equal("done", result.Status);
        Assert.NotNull(updated);
    }

    [Fact]
    public async Task Handle_MissingTodo_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        var repository = new Mock<ITodoRepository>();
        repository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Todo?)null);

        var handler = new UpdateTodoHandler(repository.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateTodoCommand(id, new UpdateTodoRequest(Title: "X")), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InvalidStatus_ThrowsValidationException()
    {
        var todo = Todo.Create("Todo", Guid.NewGuid());
        var repository = new Mock<ITodoRepository>();
        repository
            .Setup(r => r.GetByIdAsync(todo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(todo);

        var handler = new UpdateTodoHandler(repository.Object);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(
                new UpdateTodoCommand(todo.Id, new UpdateTodoRequest(Status: "invalid")),
                CancellationToken.None));
    }
}
