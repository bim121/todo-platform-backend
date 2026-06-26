using TodoPlatform.Application.Todos.Queries.GetTodos;

namespace TodoPlatform.Application.Tests.Todos.Queries;

public sealed class GetTodosQueryValidatorTests
{
    private readonly GetTodosQueryValidator _validator = new();

    [Fact]
    public void Validate_NullUserId_IsValid()
    {
        var result = _validator.Validate(new GetTodosQuery());
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_NegativeSkip_HasError()
    {
        var result = _validator.Validate(new GetTodosQuery(Guid.NewGuid(), Skip: -1));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTodosQuery.Skip));
    }

    [Fact]
    public void Validate_TakeOutOfRange_HasError()
    {
        var zero = _validator.Validate(new GetTodosQuery(Guid.NewGuid(), Take: 0));
        Assert.Contains(zero.Errors, e => e.PropertyName == nameof(GetTodosQuery.Take));

        var tooLarge = _validator.Validate(new GetTodosQuery(Guid.NewGuid(), Take: 101));
        Assert.Contains(tooLarge.Errors, e => e.PropertyName == nameof(GetTodosQuery.Take));
    }

    [Fact]
    public void Validate_ValidPaging_NoErrors()
    {
        var result = _validator.Validate(new GetTodosQuery(Guid.NewGuid(), Skip: 0, Take: 20));
        Assert.True(result.IsValid);
    }
}
