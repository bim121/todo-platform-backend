using FluentValidation;
using TodoPlatform.Application.Todos.Queries.GetTodos;

namespace TodoPlatform.Application.Todos.Queries.GetTodos;

public sealed class GetTodosQueryValidator : AbstractValidator<GetTodosQuery>
{
    public GetTodosQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("Query parameter 'userId' is required.");
    }
}
