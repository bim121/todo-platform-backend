using TodoPlatform.Application.Todos.Queries.GetTodoStats;

namespace TodoPlatform.Application.Tests.Todos.Queries;

public sealed class GetTodoStatsQueryValidatorTests
{
    private readonly GetTodoStatsQueryValidator _validator = new();

    [Fact]
    public void EmptyGuid_IsInvalid()
    {
        var result = _validator.Validate(new GetTodoStatsQuery(Guid.Empty));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTodoStatsQuery.UserId));
    }

    [Fact]
    public void NullOrValidGuid_IsValid()
    {
        Assert.True(_validator.Validate(new GetTodoStatsQuery()).IsValid);
        Assert.True(_validator.Validate(new GetTodoStatsQuery(Guid.NewGuid())).IsValid);
    }
}
