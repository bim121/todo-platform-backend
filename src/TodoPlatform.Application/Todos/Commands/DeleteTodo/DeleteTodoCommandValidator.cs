using FluentValidation;
using TodoPlatform.Application.Todos.Commands.DeleteTodo;

namespace TodoPlatform.Application.Todos.Commands.DeleteTodo;

public sealed class DeleteTodoCommandValidator : AbstractValidator<DeleteTodoCommand>
{
    public DeleteTodoCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Todo id is required.");
    }
}
