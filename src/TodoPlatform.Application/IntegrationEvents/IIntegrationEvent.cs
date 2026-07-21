namespace TodoPlatform.Application.IntegrationEvents;

/// <summary>
/// Marker for integration events published across process boundaries (RabbitMQ / MassTransit).
/// Distinct from domain events, which stay in-process.
/// </summary>
public interface IIntegrationEvent
{
    DateTimeOffset OccurredOn { get; }
}
