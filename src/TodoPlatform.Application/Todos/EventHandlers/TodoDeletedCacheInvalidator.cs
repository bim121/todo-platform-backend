using MediatR;
using Microsoft.Extensions.Logging;
using TodoPlatform.Application.Caching;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Domain.Events;

namespace TodoPlatform.Application.Todos.EventHandlers;

public sealed class TodoDeletedCacheInvalidator(
    ICacheService cache,
    ILogger<TodoDeletedCacheInvalidator> logger)
    : INotificationHandler<TodoDeletedEvent>
{
    public async Task Handle(TodoDeletedEvent notification, CancellationToken cancellationToken)
    {
        await cache.RemoveAsync(CacheKeys.TodoById(notification.TodoId), cancellationToken);
        await cache.RemoveByPrefixAsync(
            CacheKeys.TodosByUserPrefix(notification.UserId),
            cancellationToken);

        logger.LogDebug(
            "Invalidated cache after delete todo {TodoId} user {UserId}",
            notification.TodoId,
            notification.UserId);
    }
}
