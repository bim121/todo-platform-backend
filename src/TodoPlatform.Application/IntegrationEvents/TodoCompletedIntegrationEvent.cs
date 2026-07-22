namespace TodoPlatform.Application.IntegrationEvents;

public sealed record TodoCompletedIntegrationEvent(
    Guid TodoId,
    Guid UserId,
    DateTimeOffset OccurredOn) : IIntegrationEvent
{
    public const string EventTypeName = "TodoCompletedIntegrationEvent";
}
