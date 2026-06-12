using Moq;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Todos.Queries.GetTodos;
using TodoPlatform.Domain.Entities;
using TodoPlatform.Domain.Enums;

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
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(todos);

        var handler = new GetTodosQueryHandler(repository.Object);
        var result = await handler.Handle(new GetTodosQuery(userId), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("First", result[0].Title);
        Assert.Equal("low", result[0].Priority);
        Assert.Equal("in_progress", result[1].Status);
    }
}
