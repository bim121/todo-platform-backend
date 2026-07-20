using MediatR;
using Microsoft.Extensions.Logging;
using TodoPlatform.Application.Caching;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Domain.Events;

namespace TodoPlatform.Application.Todos.EventHandlers;

public sealed class TodoCreatedCacheInvalidator(
    ICacheService cache,
    ILogger<TodoCreatedCacheInvalidator> logger)
    : INotificationHandler<TodoCreatedEvent>
{
    public async Task Handle(TodoCreatedEvent notification, CancellationToken cancellationToken)
    {
        await cache.RemoveByPrefixAsync(
            CacheKeys.TodosByUserPrefix(notification.UserId),
            cancellationToken);

        logger.LogDebug(
            "Invalidated todos list cache for user {UserId} after create {TodoId}",
            notification.UserId,
            notification.TodoId);
    }
}
