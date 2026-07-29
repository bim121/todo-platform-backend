using Moq;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Services;
using TodoPlatform.Application.Tests.Support;
using TodoPlatform.Application.Todos.Queries.GetTodos;
using TodoPlatform.Application.Todos.Specifications;
using TodoPlatform.Domain.Specifications;

namespace TodoPlatform.Application.Tests.Todos.Queries;

public sealed class GetTodosQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMappedTodosForUser()
    {
        var userId = Guid.NewGuid();
        var dtos = new List<TodoDto>
        {
            new(Guid.NewGuid(), "First", false, userId, "todo", "low"),
            new(Guid.NewGuid(), "Second", false, userId, "in_progress", "high")
        };

        var repository = new Mock<ITodoRepository>();
        repository
            .Setup(r => r.ListDtosAsync(It.IsAny<Specification<TodoPlatform.Domain.Entities.Todo>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dtos);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(userId);

        var cache = new PassThroughCacheService();
        var handler = new GetTodosQueryHandler(repository.Object, currentUser.Object, cache);
        var result = await handler.Handle(new GetTodosQuery(userId), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("First", result[0].Title);
        Assert.Equal("low", result[0].Priority);
        Assert.Equal("in_progress", result[1].Status);
        Assert.Equal(1, cache.GetOrSetCalls);

        repository.Verify(
            r => r.ListDtosAsync(
                It.IsAny<Specification<TodoPlatform.Domain.Entities.Todo>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        repository.Verify(
            r => r.ListAsync(It.IsAny<Specification<TodoPlatform.Domain.Entities.Todo>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ActiveOnly_UsesCombinedSpecification()
    {
        var userId = Guid.NewGuid();
        Specification<TodoPlatform.Domain.Entities.Todo>? captured = null;

        var repository = new Mock<ITodoRepository>();
        repository
            .Setup(r => r.ListDtosAsync(It.IsAny<Specification<TodoPlatform.Domain.Entities.Todo>>(), It.IsAny<CancellationToken>()))
            .Callback<Specification<TodoPlatform.Domain.Entities.Todo>, CancellationToken>((spec, _) => captured = spec)
            .ReturnsAsync([]);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(userId);

        var handler = new GetTodosQueryHandler(
            repository.Object,
            currentUser.Object,
            new PassThroughCacheService());
        await handler.Handle(new GetTodosQuery(userId, ActiveOnly: true), CancellationToken.None);

        Assert.NotNull(captured);
        Assert.IsNotType<TodoByUserSpecification>(captured);
    }

    [Fact]
    public async Task Handle_WithPaging_UsesCombinedSpecification()
    {
        var userId = Guid.NewGuid();
        Specification<TodoPlatform.Domain.Entities.Todo>? captured = null;

        var repository = new Mock<ITodoRepository>();
        repository
            .Setup(r => r.ListDtosAsync(It.IsAny<Specification<TodoPlatform.Domain.Entities.Todo>>(), It.IsAny<CancellationToken>()))
            .Callback<Specification<TodoPlatform.Domain.Entities.Todo>, CancellationToken>((spec, _) => captured = spec)
            .ReturnsAsync([]);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(userId);

        var handler = new GetTodosQueryHandler(
            repository.Object,
            currentUser.Object,
            new PassThroughCacheService());
        await handler.Handle(new GetTodosQuery(userId, Skip: 2, Take: 5), CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(2, captured!.Skip);
        Assert.Equal(5, captured.Take);
        Assert.True(captured.OrderById);
    }
}
