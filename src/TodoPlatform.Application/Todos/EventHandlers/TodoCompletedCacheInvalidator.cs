using MediatR;
using Microsoft.Extensions.Logging;
using TodoPlatform.Application.Caching;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Domain.Events;

namespace TodoPlatform.Application.Todos.EventHandlers;

public sealed class TodoCompletedCacheInvalidator(
    ICacheService cache,
    ILogger<TodoCompletedCacheInvalidator> logger)
    : INotificationHandler<TodoCompletedEvent>
{
    public async Task Handle(TodoCompletedEvent notification, CancellationToken cancellationToken)
    {
        await InvalidateAsync(
            notification.TenantId,
            notification.TodoId,
            notification.UserId,
            cancellationToken);
        logger.LogDebug(
            "Invalidated cache after complete todo {TodoId} tenant {TenantId} user {UserId}",
            notification.TodoId,
            notification.TenantId,
            notification.UserId);
    }

    private async Task InvalidateAsync(
        Guid tenantId,
        Guid todoId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await cache.RemoveAsync(CacheKeys.TodoById(tenantId, todoId), cancellationToken);
        await cache.RemoveByPrefixAsync(CacheKeys.TodosByUserPrefix(tenantId, userId), cancellationToken);
        await cache.RemoveAsync(CacheKeys.TodoStatsByUser(tenantId, userId), cancellationToken);
    }
}
