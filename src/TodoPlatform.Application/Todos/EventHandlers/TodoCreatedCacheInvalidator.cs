using MediatR;
using Microsoft.Extensions.Logging;
using TodoPlatform.Domain.Events;

namespace TodoPlatform.Application.Todos.EventHandlers;

/// <summary>
/// Stub until B-06 (Redis cache invalidation for todos:user:{'{userId}'}).
/// </summary>
public sealed class TodoCreatedCacheInvalidator(ILogger<TodoCreatedCacheInvalidator> logger)
    : INotificationHandler<TodoCreatedEvent>
{
    public Task Handle(TodoCreatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Cache invalidation stub: todos list for user {UserId} (todo {TodoId})",
            notification.UserId,
            notification.TodoId);

        return Task.CompletedTask;
    }
}
