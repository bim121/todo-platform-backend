using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TodoPlatform.Application.Todos.Specifications;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Infrastructure.Tests.Persistence;

/// <summary>
/// Verifies specification evaluator builds relational SQL (SQLite translation).
/// </summary>
public sealed class SpecificationEvaluatorSqlTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly SpecificationEvaluator _evaluator = new();

    public SpecificationEvaluatorSqlTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
    }

    [Fact]
    public void TodoByUserSpecification_GeneratesUserIdFilterAndTitleOrder()
    {
        var userId = Guid.NewGuid();
        var sql = BuildSql(TodoListSpecification.Create(userId));

        Assert.Contains("UserId", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Title", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ActiveTodosSpecification_GeneratesCompletedFilter()
    {
        var userId = Guid.NewGuid();
        var sql = BuildSql(TodoListSpecification.Create(userId, activeOnly: true));

        Assert.Contains("UserId", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Completed", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TodoListSpecification_WithPaging_GeneratesSkipTake()
    {
        var userId = Guid.NewGuid();
        var sql = BuildSql(TodoListSpecification.Create(userId, skip: 5, take: 10));

        Assert.Contains("LIMIT", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("OFFSET", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ActiveTodosSpecification_Alone_GeneratesCompletedPredicate()
    {
        var sql = BuildSql(new ActiveTodosSpecification());

        Assert.Contains("Completed", sql, StringComparison.OrdinalIgnoreCase);

        var whereIndex = sql.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase);
        Assert.True(whereIndex >= 0);
        var whereClause = sql[whereIndex..];
        Assert.DoesNotContain("UserId =", whereClause, StringComparison.OrdinalIgnoreCase);
    }

    private string BuildSql(Domain.Specifications.Specification<Domain.Entities.Todo> specification)
    {
        var query = _evaluator.GetQuery(_db.Todos.AsQueryable(), specification);
        return query.ToQueryString();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
