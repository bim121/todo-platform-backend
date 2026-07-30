using Moq;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Exceptions;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Services;
using TodoPlatform.Application.Todos.Queries.GetTodoStats;

namespace TodoPlatform.Application.Tests.Todos.Queries;

public sealed class GetTodoStatsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsStatsFromStore()
    {
        var userId = Guid.NewGuid();
        var expected = new TodoStatsDto(userId, Total: 5, Active: 2, Completed: 3);

        var store = new Mock<ITodoStatsReadStore>();
        store
            .Setup(s => s.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(userId);

        var handler = new GetTodoStatsQueryHandler(store.Object, currentUser.Object);
        var result = await handler.Handle(new GetTodoStatsQuery(userId), CancellationToken.None);

        Assert.Equal(expected, result);
        store.Verify(s => s.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutUserId_UsesCurrentUser()
    {
        var userId = Guid.NewGuid();
        var expected = new TodoStatsDto(userId, 1, 1, 0);

        var store = new Mock<ITodoStatsReadStore>();
        store
            .Setup(s => s.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(userId);

        var handler = new GetTodoStatsQueryHandler(store.Object, currentUser.Object);
        var result = await handler.Handle(new GetTodoStatsQuery(), CancellationToken.None);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task Handle_MissingUserId_ThrowsValidationException()
    {
        var store = new Mock<ITodoStatsReadStore>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(Guid.Empty);

        var handler = new GetTodoStatsQueryHandler(store.Object, currentUser.Object);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new GetTodoStatsQuery(), CancellationToken.None));
    }
}
