using MassTransit;
using Microsoft.Extensions.Logging;
using TodoPlatform.Application.IntegrationEvents;
using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Infrastructure.Messaging.Consumers;

/// <summary>
/// B-12.8 — audit notification stub for tenant migration apply (SignalR B-13, Kafka B-16).
/// </summary>
public sealed class TenantMigrationAppliedNotificationConsumer(
    IProcessedMessageStore processedMessages,
    ILogger<TenantMigrationAppliedNotificationConsumer> logger) : IConsumer<TenantMigrationAppliedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<TenantMigrationAppliedIntegrationEvent> context)
    {
        var messageId = context.MessageId
            ?? throw new InvalidOperationException("MassTransit MessageId is required for idempotency.");

        if (!await processedMessages.TryAcquireAsync(messageId, context.CancellationToken))
        {
            logger.LogInformation(
                "Skipping duplicate TenantMigrationAppliedIntegrationEvent {MessageId} for tenant {TenantId}",
                messageId,
                context.Message.TenantId);
            return;
        }

        logger.LogInformation(
            "TenantMigrationAppliedNotification {TenantId} {Version} by {AppliedBy}",
            context.Message.TenantId,
            context.Message.Version,
            context.Message.AppliedBy);
    }
}
