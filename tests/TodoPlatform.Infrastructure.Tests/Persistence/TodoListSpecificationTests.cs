using Microsoft.EntityFrameworkCore;
using TodoPlatform.Application.Todos.Specifications;
using TodoPlatform.Domain.Entities;
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
    public void Create_WithPaging_ReturnsStableOrderedSlice()
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
        Assert.All(result, t => Assert.Equal(userId, t.UserId));
        Assert.True(result[0].Id.CompareTo(result[1].Id) < 0);

        // Same query twice → same page (stable ORDER BY Id).
        var again = evaluator
            .GetQuery(
                db.Todos.AsNoTracking().AsQueryable(),
                TodoListSpecification.Create(userId, skip: 1, take: 2))
            .Select(t => t.Id)
            .ToList();

        Assert.Equal(result.Select(t => t.Id).ToList(), again);
    }

    [Fact]
    public void Create_AlwaysOrdersById()
    {
        var spec = TodoListSpecification.Create(Guid.NewGuid());
        Assert.True(spec.OrderById);
    }

    [Fact]
    public void Evaluator_OrderById_SortsEntireUserList()
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
                TodoListSpecification.Create(userId))
            .ToList();

        Assert.Equal(5, result.Count);
        Assert.Equal(
            result.OrderBy(t => t.Id).Select(t => t.Id).ToList(),
            result.Select(t => t.Id).ToList());
    }

    [Fact]
    public void Create_ActiveOnlyAndPaging_ExcludesCompletedAndPages()
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
        Assert.False(result[0].Completed);
        Assert.Equal(userId, result[0].UserId);
    }

    private static AppDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
