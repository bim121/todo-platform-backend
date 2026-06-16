using Microsoft.EntityFrameworkCore;
using TodoPlatform.Application.Todos.Specifications;
using TodoPlatform.Domain.Entities;
using TodoPlatform.Domain.Enums;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Infrastructure.Tests.Persistence;

public sealed class TodoListSpecificationTests
{
    [Fact]
    public void Create_ActiveOnly_ExcludesCompletedTodos()
    {
        var userId = Guid.NewGuid();
        using var db = CreateDatabase();
        var active = Todo.Create("Active", userId);
        var completed = Todo.Create("Done", userId);
        completed.Complete();
        db.Todos.AddRange(active, completed, Todo.Create("Other user", Guid.NewGuid()));
        db.SaveChanges();

        var evaluator = new SpecificationEvaluator();
        var result = evaluator
            .GetQuery(
                db.Todos.AsNoTracking().AsQueryable(),
                TodoListSpecification.Create(userId, activeOnly: true))
            .ToList();

        Assert.Single(result);
        Assert.Equal("Active", result[0].Title);
    }

    [Fact]
    public void Create_WithPaging_ReturnsRequestedSlice()
    {
        var userId = Guid.NewGuid();
        using var db = CreateDatabase();
        for (var i = 0; i < 5; i++)
            db.Todos.Add(Todo.Create($"Todo {i}", userId));
        db.SaveChanges();

        var evaluator = new SpecificationEvaluator();
        var result = evaluator
            .GetQuery(
                db.Todos.AsNoTracking().AsQueryable(),
                TodoListSpecification.Create(userId, skip: 1, take: 2))
            .ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Todo 1", result[0].Title);
        Assert.Equal("Todo 2", result[1].Title);
    }

    [Fact]
    public void Create_ActiveOnlyAndPaging_CombinesFilters()
    {
        var userId = Guid.NewGuid();
        using var db = CreateDatabase();
        db.Todos.Add(Todo.Create("A", userId));
        var done = Todo.Create("B", userId);
        done.Complete();
        db.Todos.Add(done);
        db.Todos.Add(Todo.Create("C", userId));
        db.SaveChanges();

        var evaluator = new SpecificationEvaluator();
        var result = evaluator
            .GetQuery(
                db.Todos.AsNoTracking().AsQueryable(),
                TodoListSpecification.Create(userId, activeOnly: true, skip: 1, take: 1))
            .ToList();

        Assert.Single(result);
        Assert.Equal("C", result[0].Title);
    }

    private static AppDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
