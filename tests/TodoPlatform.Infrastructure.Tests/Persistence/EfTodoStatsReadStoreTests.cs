using Microsoft.EntityFrameworkCore;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Infrastructure.Tests.Persistence;

public sealed class EfTodoStatsReadStoreTests
{
    [Fact]
    public async Task GetByUserIdAsync_CountsTotalActiveCompleted()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var otherUser = Guid.NewGuid();

        db.Todos.AddRange(
            CreateTodo(userId, completed: false),
            CreateTodo(userId, completed: false),
            CreateTodo(userId, completed: true),
            CreateTodo(otherUser, completed: false));
        await db.SaveChangesAsync();

        var store = new EfTodoStatsReadStore(db);
        var stats = await store.GetByUserIdAsync(userId);

        Assert.Equal(userId, stats.UserId);
        Assert.Equal(3, stats.Total);
        Assert.Equal(2, stats.Active);
        Assert.Equal(1, stats.Completed);
    }

    [Fact]
    public async Task GetByUserIdAsync_NoTodos_ReturnsZeros()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();

        var store = new EfTodoStatsReadStore(db);
        var stats = await store.GetByUserIdAsync(userId);

        Assert.Equal(new TodoPlatform.Application.Dtos.TodoStatsDto(userId, 0, 0, 0), stats);
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static TodoPlatform.Domain.Entities.Todo CreateTodo(Guid userId, bool completed)
    {
        var todo = TodoPlatform.Domain.Entities.Todo.Create("t", userId);
        if (completed)
            todo.Complete();
        return todo;
    }
}
