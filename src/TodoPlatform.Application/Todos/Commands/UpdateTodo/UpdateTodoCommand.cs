using MediatR;
using TodoPlatform.Application.Common;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Exceptions;
using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Application.Todos.Commands.UpdateTodo;

public sealed record UpdateTodoCommand(Guid Id, UpdateTodoRequest Body) : IRequest<TodoDto>, ICommand;

public sealed class UpdateTodoHandler(ITodoRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateTodoCommand, TodoDto>
{
    public async Task<TodoDto> Handle(UpdateTodoCommand request, CancellationToken cancellationToken)
    {
        var todo = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (todo is null)
            throw new NotFoundException($"Todo '{request.Id}' was not found.");

        try
        {
            request.Body.ApplyTo(todo);
            await repository.UpdateAsync(todo, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            return TodoDto.FromEntity(todo);
        }
        catch (ArgumentException ex)
        {
            throw ValidationException.ForField("status", ex.Message);
        }
    }
}
