using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Infrastructure.Messaging;

/// <summary>
/// Polls <c>outbox_messages</c>, publishes integration events via MassTransit, then marks them processed.
/// </summary>
public sealed class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    public const int BatchSize = 100;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OutboxProcessor started (interval {IntervalSeconds}s, batch {BatchSize})",
            PollInterval.TotalSeconds,
            BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var published = await PublishPendingAsync(stoppingToken);
                if (published > 0)
                {
                    logger.LogDebug("OutboxProcessor published {Count} message(s)", published);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "OutboxProcessor batch failed; will retry on next poll");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Publishes up to <see cref="BatchSize"/> unprocessed outbox rows. Exposed for tests.
    /// </summary>
    public async Task<int> PublishPendingAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var pending = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        var published = 0;

        foreach (var message in pending)
        {
            try
            {
                var integrationEvent = IntegrationEventPayloadDeserializer.Deserialize(message.Type, message.Payload);
                if (integrationEvent is null)
                {
                    logger.LogWarning(
                        "Unknown or invalid outbox payload {OutboxId} type {Type}; marking processed to avoid poison loop",
                        message.Id,
                        message.Type);
                    message.ProcessedAt = DateTimeOffset.UtcNow;
                    await db.SaveChangesAsync(cancellationToken);
                    continue;
                }

                // Stable MessageId = outbox row Id so re-publish after crash is idempotent for consumers.
                await publishEndpoint.Publish(
                    integrationEvent,
                    integrationEvent.GetType(),
                    new SetMessageIdPipe(message.Id),
                    cancellationToken);

                message.ProcessedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
                published++;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to publish outbox message {OutboxId} type {Type}; leaving unprocessed for retry",
                    message.Id,
                    message.Type);
                db.ChangeTracker.Clear();
            }
        }

        return published;
    }

    private sealed class SetMessageIdPipe(Guid messageId) : IPipe<PublishContext>
    {
        public Task Send(PublishContext context)
        {
            context.MessageId = messageId;
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context)
        {
        }
    }
}
