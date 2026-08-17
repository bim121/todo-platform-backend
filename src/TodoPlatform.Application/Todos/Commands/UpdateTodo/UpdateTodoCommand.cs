using MediatR;
using TodoPlatform.Application.Caching;
using TodoPlatform.Application.Common;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Exceptions;
using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Application.Todos.Commands.UpdateTodo;

public sealed record UpdateTodoCommand(Guid Id, UpdateTodoRequest Body) : IRequest<TodoDto>, ICommand;

public sealed class UpdateTodoHandler(
    ITodoRepository repository,
    ICacheService cache)
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

            // Title/status updates may not raise domain events — invalidate explicitly.
            // TodoCompletedEvent also invalidates when Complete() runs (double-remove is fine).
            await cache.RemoveAsync(CacheKeys.TodoById(todo.TenantId, todo.Id), cancellationToken);
            await cache.RemoveByPrefixAsync(
                CacheKeys.TodosByUserPrefix(todo.TenantId, todo.UserId),
                cancellationToken);
            await cache.RemoveAsync(CacheKeys.TodoStatsByUser(todo.TenantId, todo.UserId), cancellationToken);

            return TodoDto.FromEntity(todo);
        }
        catch (ArgumentException ex)
        {
            throw ValidationException.ForField("status", ex.Message);
        }
    }
}
