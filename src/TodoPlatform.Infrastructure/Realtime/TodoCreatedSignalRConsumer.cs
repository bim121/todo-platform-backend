using MassTransit;
using Microsoft.Extensions.Logging;
using TodoPlatform.Application.IntegrationEvents;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Realtime;

namespace TodoPlatform.Infrastructure.Realtime;

/// <summary>B-13.4 — MassTransit → SignalR for todo created (group-scoped, not global).</summary>
public sealed class TodoCreatedSignalRConsumer(
    ITodoRealtimeNotifier notifier,
    ILogger<TodoCreatedSignalRConsumer> logger) : IConsumer<TodoCreatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<TodoCreatedIntegrationEvent> context)
    {
        var message = context.Message;
        await notifier.NotifyCreatedAsync(
            message.TenantId,
            message.UserId,
            new TodoRealtimeMessage(
                message.TodoId,
                message.Title,
                message.Completed,
                message.OccurredOn.ToUnixTimeMilliseconds()),
            context.CancellationToken);

        logger.LogDebug(
            "SignalR TodoCreated pushed todo={TodoId} tenant={TenantId} user={UserId}",
            message.TodoId,
            message.TenantId,
            message.UserId);
    }
}
