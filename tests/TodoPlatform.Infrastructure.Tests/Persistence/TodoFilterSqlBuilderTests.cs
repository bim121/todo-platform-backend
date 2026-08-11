using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Infrastructure.Tests.Persistence;

public sealed class TodoFilterSqlBuilderTests
{
    [Theory]
    [InlineData("status", true)]
    [InlineData("priority", true)]
    [InlineData("completed", true)]
    [InlineData("search", true)]
    [InlineData("drop table", false)]
    [InlineData("Title", false)]
    [InlineData("userId", true)]
    public void IsAllowedFilterKey_WhitelistOnly(string key, bool allowed)
    {
        Assert.Equal(allowed, TodoFilterSqlBuilder.IsAllowedFilterKey(key));
    }

    [Fact]
    public void Build_IncludesOnlyProvidedFilters_AsParameters()
    {
        var userId = Guid.NewGuid();
        var built = TodoFilterSqlBuilder.Build(
            userId,
            statusApi: "in_progress",
            priorityApi: "high",
            completed: false,
            search: "milk_100%",
            skip: 10,
            take: 5);

        Assert.Contains("""AND "Status" = @Status""", built.CountSql, StringComparison.Ordinal);
        Assert.Contains("""AND "Priority" = @Priority""", built.PageSql, StringComparison.Ordinal);
        Assert.Contains("""AND "Completed" = @Completed""", built.PageSql, StringComparison.Ordinal);
        Assert.Contains("""ILIKE @Search""", built.PageSql, StringComparison.Ordinal);
        Assert.Contains("OFFSET @Skip", built.PageSql, StringComparison.Ordinal);

        Assert.Equal("InProgress", built.Parameters.Get<string>("Status"));
        Assert.Equal("High", built.Parameters.Get<string>("Priority"));
        Assert.False(built.Parameters.Get<bool>("Completed"));
        Assert.Equal("%milk\\_100\\%%", built.Parameters.Get<string>("Search"));
        Assert.Equal(10, built.Parameters.Get<int>("Skip"));
        Assert.Equal(5, built.Parameters.Get<int>("Take"));
    }

    [Fact]
    public void Build_WithoutOptionalFilters_OnlyUserIdPredicate()
    {
        var built = TodoFilterSqlBuilder.Build(
            Guid.NewGuid(),
            statusApi: null,
            priorityApi: null,
            completed: null,
            search: null,
            skip: 0,
            take: 20);

        Assert.DoesNotContain("@Status", built.CountSql, StringComparison.Ordinal);
        Assert.DoesNotContain("@Priority", built.CountSql, StringComparison.Ordinal);
        Assert.DoesNotContain("@Completed", built.CountSql, StringComparison.Ordinal);
        Assert.DoesNotContain("@Search", built.CountSql, StringComparison.Ordinal);
        Assert.Contains("""WHERE "UserId" = @UserId""", built.CountSql, StringComparison.Ordinal);
    }
}
