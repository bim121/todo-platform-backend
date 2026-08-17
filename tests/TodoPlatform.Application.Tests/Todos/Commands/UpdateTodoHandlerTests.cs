using Moq;
using TodoPlatform.Application.Caching;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Exceptions;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Tests.Support;
using TodoPlatform.Application.Todos.Commands.UpdateTodo;
using TodoPlatform.Domain.Entities;

namespace TodoPlatform.Application.Tests.Todos.Commands;

public sealed class UpdateTodoHandlerTests
{
    [Fact]
    public async Task Handle_ExistingTodo_UpdatesAndInvalidatesCache()
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

        var cache = new PassThroughCacheService();
        var handler = new UpdateTodoHandler(repository.Object, cache);
        var result = await handler.Handle(
            new UpdateTodoCommand(todo.Id, new UpdateTodoRequest(Title: "New title", Completed: true)),
            CancellationToken.None);

        Assert.Equal("New title", result.Title);
        Assert.True(result.Completed);
        Assert.Equal("done", result.Status);
        Assert.NotNull(updated);
        Assert.Contains(CacheKeys.TodoById(todo.TenantId, todo.Id), cache.RemovedKeys);
        Assert.Contains(CacheKeys.TodosByUserPrefix(todo.TenantId, todo.UserId), cache.RemovedPrefixes);
    }

    [Fact]
    public async Task Handle_MissingTodo_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        var repository = new Mock<ITodoRepository>();
        repository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Todo?)null);

        var handler = new UpdateTodoHandler(repository.Object, new PassThroughCacheService());

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

        var handler = new UpdateTodoHandler(repository.Object, new PassThroughCacheService());

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(
                new UpdateTodoCommand(todo.Id, new UpdateTodoRequest(Status: "invalid")),
                CancellationToken.None));
    }
}
