using TodoPlatform.Application.Todos.Queries.GetTodosWithFilters;

namespace TodoPlatform.Application.Tests.Todos.Queries;

public sealed class GetTodosWithFiltersQueryValidatorTests
{
    private readonly GetTodosWithFiltersQueryValidator _validator = new();

    [Fact]
    public void ValidFilters_Pass()
    {
        var result = _validator.Validate(
            new GetTodosWithFiltersQuery(
                Guid.NewGuid(),
                Status: "todo",
                Priority: "high",
                Completed: false,
                Search: "buy",
                Skip: 0,
                Take: 20));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void InvalidStatus_Fails()
    {
        var result = _validator.Validate(new GetTodosWithFiltersQuery(Status: "nope"));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTodosWithFiltersQuery.Status));
    }

    [Fact]
    public void ContradictoryCompletedAndStatus_Fails()
    {
        var doneButActive = _validator.Validate(
            new GetTodosWithFiltersQuery(Status: "done", Completed: false));
        Assert.False(doneButActive.IsValid);

        var todoButCompleted = _validator.Validate(
            new GetTodosWithFiltersQuery(Status: "todo", Completed: true));
        Assert.False(todoButCompleted.IsValid);
    }

    [Fact]
    public void ConsistentCompletedAndStatus_Pass()
    {
        Assert.True(_validator.Validate(
            new GetTodosWithFiltersQuery(Status: "done", Completed: true)).IsValid);
        Assert.True(_validator.Validate(
            new GetTodosWithFiltersQuery(Status: "todo", Completed: false)).IsValid);
    }

    [Fact]
    public void TakeOutOfRange_Fails()
    {
        var result = _validator.Validate(new GetTodosWithFiltersQuery(Take: 0));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTodosWithFiltersQuery.Take));
    }
}
