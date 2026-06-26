using MediatR;
using TodoPlatform.Application.Common;
using TodoPlatform.Application.Exceptions;
using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Application.Todos.Commands.DeleteTodo;

public sealed record DeleteTodoCommand(Guid Id) : IRequest, ICommand;

public sealed class DeleteTodoHandler(ITodoRepository repository) : IRequestHandler<DeleteTodoCommand>
{
    public async Task Handle(DeleteTodoCommand request, CancellationToken cancellationToken)
    {
        var deleted = await repository.DeleteAsync(request.Id, cancellationToken);
        if (!deleted)
            throw new NotFoundException($"Todo '{request.Id}' was not found.");
    }
}
