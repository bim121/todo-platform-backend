using Microsoft.EntityFrameworkCore;
using TodoPlatform.Application.Todos.Specifications;
using TodoPlatform.Domain.Entities;
using TodoPlatform.Domain.Enums;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Infrastructure.Tests.Persistence;

public sealed class SpecificationEvaluatorTests
{
    [Fact]
    public void GetQuery_TodoByUserSpecification_FiltersAndOrders()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var todos = new[]
        {
            Todo.Create("Zebra", userId),
            Todo.Create("Alpha", userId),
            Todo.Create("Other", otherUserId)
        };

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new AppDbContext(options);
        db.Todos.AddRange(todos);
        db.SaveChanges();

        var evaluator = new SpecificationEvaluator();
        var spec = new TodoByUserSpecification(userId);
        var result = evaluator
            .GetQuery(db.Todos.AsNoTracking().AsQueryable(), spec)
            .ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Alpha", result[0].Title);
        Assert.Equal("Zebra", result[1].Title);
        Assert.All(result, todo => Assert.Equal(userId, todo.UserId));
    }

    [Fact]
    public void GetQuery_AppliesPaging()
    {
        var userId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new AppDbContext(options);
        for (var i = 0; i < 5; i++)
            db.Todos.Add(Todo.Create($"Todo {i}", userId, TodoStatus.Todo, TodoPriority.Medium));
        db.SaveChanges();

        var evaluator = new SpecificationEvaluator();
        var spec = new PagedTodoByUserSpecification(userId, skip: 1, take: 2);
        var result = evaluator
            .GetQuery(db.Todos.AsNoTracking().AsQueryable(), spec)
            .ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Todo 1", result[0].Title);
        Assert.Equal("Todo 2", result[1].Title);
    }

    private sealed class PagedTodoByUserSpecification : Domain.Specifications.Specification<Todo>
    {
        public PagedTodoByUserSpecification(Guid userId, int skip, int take)
        {
            Criteria = todo => todo.UserId == userId;
            ApplyOrderBy(todo => todo.Title);
            ApplyPaging(skip, take);
        }
    }
}
