using Microsoft.EntityFrameworkCore;
using TodoPlatform.Domain.Entities;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Infrastructure.Tests.Persistence;

public sealed class EfSystemStatsReadStoreTests
{
    [Fact]
    public async Task GetAsync_ComputesAvgRounded()
    {
        await using var db = CreateDb();
        var user1 = User.Register("u1@example.com", "hash", "U1");
        var user2 = User.Register("u2@example.com", "hash", "U2");

        db.Users.AddRange(user1, user2);
        db.Todos.AddRange(
            Todo.Create("a", user1.Id),
            Todo.Create("b", user1.Id),
            Todo.Create("c", user2.Id));
        await db.SaveChangesAsync();

        var stats = await new EfSystemStatsReadStore(db).GetAsync();

        Assert.Equal(2, stats.TotalUsers);
        Assert.Equal(3, stats.TotalTodos);
        Assert.Equal(1.50m, stats.AvgTodosPerUser);
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
