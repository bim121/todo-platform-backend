using FluentValidation;

namespace TodoPlatform.Application.Todos.Queries.GetTodoStats;

public sealed class GetTodoStatsQueryValidator : AbstractValidator<GetTodoStatsQuery>
{
    public GetTodoStatsQueryValidator()
    {
        RuleFor(x => x.UserId)
            .Must(id => id is null || id != Guid.Empty)
            .WithMessage("Query parameter 'userId' must not be an empty GUID.");
    }
}
