using MediatR;
using Microsoft.Extensions.Logging;
using TodoPlatform.Domain.Events;

namespace TodoPlatform.Application.Todos.EventHandlers;

/// <summary>
/// Audit trail for todo creation (full audit stream in B-16).
/// </summary>
public sealed class TodoCreatedAuditHandler(ILogger<TodoCreatedAuditHandler> logger)
    : INotificationHandler<TodoCreatedEvent>
{
    public Task Handle(TodoCreatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Todo created: {TodoId} for user {UserId}, title {Title}, at {OccurredOn}",
            notification.TodoId,
            notification.UserId,
            notification.Title,
            notification.OccurredOn);

        return Task.CompletedTask;
    }
}
