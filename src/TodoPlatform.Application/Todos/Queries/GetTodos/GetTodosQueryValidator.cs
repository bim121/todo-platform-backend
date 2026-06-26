using FluentValidation;
using TodoPlatform.Application.Todos.Queries.GetTodos;

namespace TodoPlatform.Application.Todos.Queries.GetTodos;

public sealed class GetTodosQueryValidator : AbstractValidator<GetTodosQuery>
{
    public const int MaxPageSize = 100;

    public GetTodosQueryValidator()
    {
        RuleFor(x => x.Skip)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Skip.HasValue)
            .WithMessage("Query parameter 'skip' must be greater than or equal to 0.");

        RuleFor(x => x.Take)
            .InclusiveBetween(1, MaxPageSize)
            .When(x => x.Take.HasValue)
            .WithMessage($"Query parameter 'take' must be between 1 and {MaxPageSize}.");
    }
}
