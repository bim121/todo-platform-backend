using Microsoft.EntityFrameworkCore;
using TodoPlatform.Application.Todos.Specifications;
using TodoPlatform.Domain.Entities;
using TodoPlatform.Domain.Enums;
using TodoPlatform.Infrastructure.Persistence;
using TodoPlatform.Infrastructure.Repositories;

namespace TodoPlatform.Infrastructure.Tests.Persistence;

public sealed class TodoRepositoryListDtosTests
{
    [Fact]
    public async Task ListDtosAsync_ProjectsStatusAndPriorityInQuery()
    {
        var userId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new AppDbContext(options);
        db.Todos.Add(Todo.Create("Low", userId, TodoStatus.Todo, TodoPriority.Low));
        db.Todos.Add(Todo.Create("High", userId, TodoStatus.InProgress, TodoPriority.High));
        await db.SaveChangesAsync();

        var repository = new TodoRepository(db, new SpecificationEvaluator());
        var dtos = await repository.ListDtosAsync(TodoListSpecification.Create(userId));

        Assert.Equal(2, dtos.Count);
        Assert.Contains(dtos, d => d is { Title: "Low", Status: "todo", Priority: "low" });
        Assert.Contains(dtos, d => d is { Title: "High", Status: "in_progress", Priority: "high" });
    }
}
