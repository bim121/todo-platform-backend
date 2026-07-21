using MassTransit;
using Microsoft.Extensions.Logging;
using TodoPlatform.Application.IntegrationEvents;
using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Infrastructure.Messaging.Consumers;

/// <summary>
/// Simulated email on todo created. Side effect is async via outbox → RabbitMQ (B-07).
/// Full SMTP / Mailhog wiring is B-07.7.
/// </summary>
public sealed class SendTodoCreatedEmailConsumer(
    IProcessedMessageStore processedMessages,
    ILogger<SendTodoCreatedEmailConsumer> logger) : IConsumer<TodoCreatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<TodoCreatedIntegrationEvent> context)
    {
        var messageId = context.MessageId
            ?? throw new InvalidOperationException("MassTransit MessageId is required for idempotency.");

        if (!await processedMessages.TryAcquireAsync(messageId, context.CancellationToken))
        {
            logger.LogInformation(
                "Skipping duplicate TodoCreatedIntegrationEvent {MessageId} for todo {TodoId}",
                messageId,
                context.Message.TodoId);
            return;
        }

        // Simulated SMTP — structured log stands in for email send until B-07.7.
        logger.LogInformation(
            "TodoCreatedEmailSent {TodoId} {UserId} {Title} {MessageId}",
            context.Message.TodoId,
            context.Message.UserId,
            context.Message.Title,
            messageId);
    }
}
