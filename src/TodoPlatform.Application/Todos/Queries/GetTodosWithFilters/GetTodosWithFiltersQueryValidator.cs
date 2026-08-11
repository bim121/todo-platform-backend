using FluentValidation;
using TodoPlatform.Application.Mapping;
using TodoPlatform.Application.Todos.Queries.GetTodos;
using TodoPlatform.Domain.Enums;

namespace TodoPlatform.Application.Todos.Queries.GetTodosWithFilters;

public sealed class GetTodosWithFiltersQueryValidator : AbstractValidator<GetTodosWithFiltersQuery>
{
    public const int MaxSearchLength = 200;

    public GetTodosWithFiltersQueryValidator()
    {
        RuleFor(x => x.Skip)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Query parameter 'skip' must be greater than or equal to 0.");

        RuleFor(x => x.Take)
            .InclusiveBetween(1, GetTodosQueryValidator.MaxPageSize)
            .WithMessage($"Query parameter 'take' must be between 1 and {GetTodosQueryValidator.MaxPageSize}.");

        RuleFor(x => x.Status)
            .Must(s => s is null || TodoContractMapper.TryParseStatus(s, out _))
            .WithMessage("Query parameter 'status' must be one of: todo, in_progress, done.");

        RuleFor(x => x.Priority)
            .Must(p => p is null || TodoContractMapper.TryParsePriority(p, out _))
            .WithMessage("Query parameter 'priority' must be one of: low, medium, high.");

        RuleFor(x => x.Search)
            .MaximumLength(MaxSearchLength)
            .When(x => x.Search is not null)
            .WithMessage($"Query parameter 'search' must be at most {MaxSearchLength} characters.");

        // Invalid combo: completed flag contradicts status (B-10.5 ProblemDetails).
        RuleFor(x => x)
            .Must(NotContradictCompletedAndStatus)
            .WithMessage(
                "Query parameters 'completed' and 'status' contradict each other "
                + "(e.g. completed=true with status=todo, or completed=false with status=done).")
            .WithName("completed");
    }

    private static bool NotContradictCompletedAndStatus(GetTodosWithFiltersQuery query)
    {
        if (query.Completed is null || string.IsNullOrWhiteSpace(query.Status))
            return true;

        if (!TodoContractMapper.TryParseStatus(query.Status, out var status))
            return true; // status rule reports the enum error

        return query.Completed.Value
            ? status == TodoStatus.Done
            : status != TodoStatus.Done;
    }
}
