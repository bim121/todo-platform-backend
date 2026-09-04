using MassTransit;
using Microsoft.Extensions.Logging;
using TodoPlatform.Application.IntegrationEvents;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Realtime;

namespace TodoPlatform.Infrastructure.Realtime;

/// <summary>B-13.5 — MassTransit → SignalR for todo deleted.</summary>
public sealed class TodoDeletedSignalRConsumer(
    ITodoRealtimeNotifier notifier,
    ILogger<TodoDeletedSignalRConsumer> logger) : IConsumer<TodoDeletedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<TodoDeletedIntegrationEvent> context)
    {
        var message = context.Message;
        await notifier.NotifyDeletedAsync(
            message.TenantId,
            message.UserId,
            new TodoRealtimeMessage(
                message.TodoId,
                message.Title,
                message.Completed,
                message.OccurredOn.ToUnixTimeMilliseconds()),
            context.CancellationToken);

        logger.LogDebug(
            "SignalR TodoDeleted pushed todo={TodoId} tenant={TenantId} user={UserId}",
            message.TodoId,
            message.TenantId,
            message.UserId);
    }
}
