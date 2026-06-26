using FluentValidation;
using TodoPlatform.Application.Todos.Queries.GetTodoById;

namespace TodoPlatform.Application.Todos.Queries.GetTodoById;

public sealed class GetTodoByIdQueryValidator : AbstractValidator<GetTodoByIdQuery>
{
    public GetTodoByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Todo id is required.");
    }
}
