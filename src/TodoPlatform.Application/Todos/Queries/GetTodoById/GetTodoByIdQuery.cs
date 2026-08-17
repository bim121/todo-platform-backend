using MediatR;
using TodoPlatform.Application.Caching;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Exceptions;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Tenancy;

namespace TodoPlatform.Application.Todos.Queries.GetTodoById;

public sealed record GetTodoByIdQuery(Guid Id) : IRequest<TodoDto>;

public sealed class GetTodoByIdQueryHandler(
    ITodoRepository repository,
    ICacheService cache,
    ITenantContext tenantContext)
    : IRequestHandler<GetTodoByIdQuery, TodoDto>
{
    public Task<TodoDto> Handle(GetTodoByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.RequireTenantId();
        return cache.GetOrSetAsync(
            CacheKeys.TodoById(tenantId, request.Id),
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
}
