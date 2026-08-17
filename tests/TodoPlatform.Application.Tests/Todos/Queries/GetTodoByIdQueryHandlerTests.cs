using Moq;
using TodoPlatform.Application.Exceptions;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Tests.Support;
using TodoPlatform.Application.Todos.Queries.GetTodoById;
using TodoPlatform.Domain.Entities;

namespace TodoPlatform.Application.Tests.Todos.Queries;

public sealed class GetTodoByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ExistingTodo_ReturnsDto()
    {
        var todo = Todo.Create("Existing", Guid.NewGuid());
        var repository = new Mock<ITodoRepository>();
        repository
            .Setup(r => r.GetByIdAsync(todo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(todo);

        var handler = new GetTodoByIdQueryHandler(
            repository.Object,
            new PassThroughCacheService(),
            TestTenantContext.Default);
        var result = await handler.Handle(new GetTodoByIdQuery(todo.Id), CancellationToken.None);

        Assert.Equal(todo.Id, result.Id);
        Assert.Equal("Existing", result.Title);
    }

    [Fact]
    public async Task Handle_MissingTodo_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        var repository = new Mock<ITodoRepository>();
        repository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Todo?)null);

        var handler = new GetTodoByIdQueryHandler(
            repository.Object,
            new PassThroughCacheService(),
            TestTenantContext.Default);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetTodoByIdQuery(id), CancellationToken.None));
    }
}
