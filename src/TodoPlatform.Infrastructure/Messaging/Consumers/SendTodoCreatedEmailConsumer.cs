using MassTransit;
using Microsoft.Extensions.Logging;
using TodoPlatform.Application.IntegrationEvents;
using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Infrastructure.Messaging.Consumers;

/// <summary>
/// Simulated email on todo created. Side effect is async via outbox → RabbitMQ (B-07).
/// Optional SMTP delivery targets Mailhog when <c>Smtp:Enabled</c> is true.
/// </summary>
public sealed class SendTodoCreatedEmailConsumer(
    IProcessedMessageStore processedMessages,
    IUserRepository users,
    IEmailSender emailSender,
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

        var user = await users.GetByIdAsync(context.Message.UserId, context.CancellationToken);
        var userEmail = user?.Email ?? "(unknown)";

        await emailSender.SendAsync(
            to: userEmail,
            subject: $"Todo created: {context.Message.Title}",
            body: $"Your todo \"{context.Message.Title}\" (id {context.Message.TodoId}) was created.",
            cancellationToken: context.CancellationToken);

        logger.LogInformation(
            "TodoCreatedEmailSent {TodoId} {UserEmail}",
            context.Message.TodoId,
            userEmail);
    }
}
