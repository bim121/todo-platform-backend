using MediatR;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Exceptions;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Domain.Entities;

namespace TodoPlatform.Application.Todos.Commands.CreateTodo;

public sealed record CreateTodoCommand(string Title, Guid UserId) : IRequest<TodoDto>;

public sealed class CreateTodoHandler(ITodoRepository repository)
    : IRequestHandler<CreateTodoCommand, TodoDto>
{
    public async Task<TodoDto> Handle(CreateTodoCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw ValidationException.ForField("title", "Title is required.");

        if (request.UserId == Guid.Empty)
            throw ValidationException.ForField("userId", "UserId is required.");

        try
        {
            var todo = Todo.Create(request.Title, request.UserId);
            await repository.AddAsync(todo, cancellationToken);
            return TodoDto.FromEntity(todo);
        }
        catch (ArgumentException ex)
        {
            throw ValidationException.ForField("title", ex.Message);
        }
    }
}
