using Microsoft.EntityFrameworkCore;
using TodoPlatform.Domain.Entities;
using TodoPlatform.Domain.Enums;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Infrastructure.Tests.Persistence;

public sealed class EfTodoFilterReadStoreTests
{
    [Fact]
    public async Task SearchAsync_FiltersAndPages()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();

        db.Todos.AddRange(
            Todo.Create("Buy milk", userId, TodoStatus.Todo, TodoPriority.Low),
            Todo.Create("Buy eggs", userId, TodoStatus.InProgress, TodoPriority.High),
            Todo.Create("Other", userId, TodoStatus.Done, TodoPriority.Medium));
        var done = Todo.Create("Ship it", userId, TodoStatus.Done, TodoPriority.High);
        done.Complete();
        db.Todos.Add(done);
        await db.SaveChangesAsync();

        var store = new EfTodoFilterReadStore(db);
        var page = await store.SearchAsync(
            userId,
            status: null,
            priority: "high",
            completed: null,
            search: "Buy",
            skip: 0,
            take: 10);

        Assert.Equal(1, page.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal("Buy eggs", page.Items[0].Title);
        Assert.Equal("in_progress", page.Items[0].Status);
        Assert.Equal("high", page.Items[0].Priority);
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
