using Moq;
using TodoPlatform.Application.Exceptions;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Todos.Commands.CreateTodo;
using TodoPlatform.Domain.Entities;

namespace TodoPlatform.Application.Tests.Todos.Commands;

public sealed class CreateTodoHandlerTests
{
    [Fact]
    public async Task Handle_CreatesTodoAndReturnsDto()
    {
        var userId = Guid.NewGuid();
        Todo? saved = null;

        var repository = new Mock<ITodoRepository>();
        repository
            .Setup(r => r.AddAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()))
            .Callback<Todo, CancellationToken>((todo, _) => saved = todo)
            .ReturnsAsync((Todo todo, CancellationToken _) => todo);

        var handler = new CreateTodoHandler(repository.Object);
        var result = await handler.Handle(new CreateTodoCommand("Learn MediatR", userId), CancellationToken.None);

        Assert.Equal("Learn MediatR", result.Title);
        Assert.Equal(userId, result.UserId);
        Assert.False(result.Completed);
        Assert.NotNull(saved);
        Assert.Equal("Learn MediatR", saved!.Title);
    }

    [Fact]
    public async Task Handle_EmptyTitle_ThrowsValidationException()
    {
        var repository = new Mock<ITodoRepository>();
        var handler = new CreateTodoHandler(repository.Object);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new CreateTodoCommand("   ", Guid.NewGuid()), CancellationToken.None));
    }
}
