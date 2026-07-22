using MassTransit;
using Microsoft.Extensions.Logging;
using TodoPlatform.Application.IntegrationEvents;
using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Infrastructure.Messaging.Consumers;

/// <summary>
/// Stub for B-13 SignalR bridge — reacts to todo completed without pushing to clients yet.
/// Future: Kafka audit stream — B-16.
/// </summary>
public sealed class TodoCompletedNotificationConsumer(
    IProcessedMessageStore processedMessages,
    ILogger<TodoCompletedNotificationConsumer> logger) : IConsumer<TodoCompletedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<TodoCompletedIntegrationEvent> context)
    {
        var messageId = context.MessageId
            ?? throw new InvalidOperationException("MassTransit MessageId is required for idempotency.");

        if (!await processedMessages.TryAcquireAsync(messageId, context.CancellationToken))
        {
            logger.LogInformation(
                "Skipping duplicate TodoCompletedIntegrationEvent {MessageId} for todo {TodoId}",
                messageId,
                context.Message.TodoId);
            return;
        }

        // B-13: publish to SignalR hub so connected clients refresh completed todos.
        // B-16: also forward to Kafka audit stream.
        logger.LogInformation(
            "TodoCompletedNotificationStub {TodoId} {UserId}",
            context.Message.TodoId,
            context.Message.UserId);
    }
}
