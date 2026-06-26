using Moq;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Services;
using TodoPlatform.Application.Todos.Queries.GetTodos;
using TodoPlatform.Application.Todos.Specifications;
using TodoPlatform.Domain.Entities;
using TodoPlatform.Domain.Enums;
using TodoPlatform.Domain.Specifications;

namespace TodoPlatform.Application.Tests.Todos.Queries;

public sealed class GetTodosQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMappedTodosForUser()
    {
        var userId = Guid.NewGuid();
        var todos = new List<Todo>
        {
            Todo.Create("First", userId, TodoStatus.Todo, TodoPriority.Low),
            Todo.Create("Second", userId, TodoStatus.InProgress, TodoPriority.High)
        };

        var repository = new Mock<ITodoRepository>();
        repository
            .Setup(r => r.ListAsync(It.IsAny<Specification<Todo>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(todos);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(userId);

        var handler = new GetTodosQueryHandler(repository.Object, currentUser.Object);
        var result = await handler.Handle(new GetTodosQuery(userId), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("First", result[0].Title);
        Assert.Equal("low", result[0].Priority);
        Assert.Equal("in_progress", result[1].Status);

        repository.Verify(
            r => r.ListAsync(
                It.Is<Specification<Todo>>(s => s is TodoByUserSpecification),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ActiveOnly_UsesCombinedSpecification()
    {
        var userId = Guid.NewGuid();
        Specification<Todo>? captured = null;

        var repository = new Mock<ITodoRepository>();
        repository
            .Setup(r => r.ListAsync(It.IsAny<Specification<Todo>>(), It.IsAny<CancellationToken>()))
            .Callback<Specification<Todo>, CancellationToken>((spec, _) => captured = spec)
            .ReturnsAsync([]);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(userId);

        var handler = new GetTodosQueryHandler(repository.Object, currentUser.Object);
        await handler.Handle(new GetTodosQuery(userId, ActiveOnly: true), CancellationToken.None);

        Assert.NotNull(captured);
        Assert.IsNotType<TodoByUserSpecification>(captured);
    }

    [Fact]
    public async Task Handle_WithPaging_UsesCombinedSpecification()
    {
        var userId = Guid.NewGuid();
        Specification<Todo>? captured = null;

        var repository = new Mock<ITodoRepository>();
        repository
            .Setup(r => r.ListAsync(It.IsAny<Specification<Todo>>(), It.IsAny<CancellationToken>()))
            .Callback<Specification<Todo>, CancellationToken>((spec, _) => captured = spec)
            .ReturnsAsync([]);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(userId);

        var handler = new GetTodosQueryHandler(repository.Object, currentUser.Object);
        await handler.Handle(new GetTodosQuery(userId, Skip: 2, Take: 5), CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(2, captured!.Skip);
        Assert.Equal(5, captured.Take);
    }
}
