using MassTransit;
using Microsoft.Extensions.Logging;
using TodoPlatform.Application.IntegrationEvents;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Realtime;

namespace TodoPlatform.Infrastructure.Realtime;

/// <summary>B-13.5 — MassTransit → SignalR for todo updated.</summary>
public sealed class TodoUpdatedSignalRConsumer(
    ITodoRealtimeNotifier notifier,
    ILogger<TodoUpdatedSignalRConsumer> logger) : IConsumer<TodoUpdatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<TodoUpdatedIntegrationEvent> context)
    {
        var message = context.Message;
        await notifier.NotifyUpdatedAsync(
            message.TenantId,
            message.UserId,
            new TodoRealtimeMessage(
                message.TodoId,
                message.Title,
                message.Completed,
                message.OccurredOn.ToUnixTimeMilliseconds()),
            context.CancellationToken);

        logger.LogDebug(
            "SignalR TodoUpdated pushed todo={TodoId} tenant={TenantId} user={UserId}",
            message.TodoId,
            message.TenantId,
            message.UserId);
    }
}
