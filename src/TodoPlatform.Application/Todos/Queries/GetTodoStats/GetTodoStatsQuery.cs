using MediatR;
using TodoPlatform.Application.Caching;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Exceptions;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Services;
using TodoPlatform.Application.Tenancy;

namespace TodoPlatform.Application.Todos.Queries.GetTodoStats;

public sealed record GetTodoStatsQuery(Guid? UserId = null) : IRequest<TodoStatsDto>;

public sealed class GetTodoStatsQueryHandler(
    ITodoStatsReadStore statsStore,
    ICurrentUserService currentUser,
    ICacheService cache,
    ITenantContext tenantContext)
    : IRequestHandler<GetTodoStatsQuery, TodoStatsDto>
{
    public async Task<TodoStatsDto> Handle(
        GetTodoStatsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = request.UserId ?? currentUser.UserId;
        if (userId == Guid.Empty)
        {
            throw new ValidationException(
                "User id is required.",
                new Dictionary<string, string[]>
                {
                    ["userId"] = ["Query parameter 'userId' is required when the caller is not authenticated."]
                });
        }

        var tenantId = tenantContext.RequireTenantId();
        return await cache.GetOrSetAsync(
            CacheKeys.TodoStatsByUser(tenantId, userId),
            ct => statsStore.GetByUserIdAsync(userId, ct),
            CacheTtl.TodoStats,
            cancellationToken);
    }
}
