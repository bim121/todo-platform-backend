using MediatR;
using TodoPlatform.Application.Caching;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Exceptions;
using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Application.Todos.Queries.GetTodoById;

public sealed record GetTodoByIdQuery(Guid Id) : IRequest<TodoDto>;

public sealed class GetTodoByIdQueryHandler(
    ITodoRepository repository,
    ICacheService cache)
    : IRequestHandler<GetTodoByIdQuery, TodoDto>
{
    public Task<TodoDto> Handle(GetTodoByIdQuery request, CancellationToken cancellationToken) =>
        cache.GetOrSetAsync(
            CacheKeys.TodoById(request.Id),
            async ct =>
            {
                var todo = await repository.GetByIdAsync(request.Id, ct);
                if (todo is null)
                    throw new NotFoundException($"Todo '{request.Id}' was not found.");

                return TodoDto.FromEntity(todo);
            },
            CacheTtl.TodoById,
            cancellationToken);
}
