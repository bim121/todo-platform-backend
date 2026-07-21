namespace TodoPlatform.Application.IntegrationEvents;

public sealed record TodoCreatedIntegrationEvent(
    Guid TodoId,
    Guid UserId,
    string Title,
    DateTimeOffset OccurredOn) : IIntegrationEvent
{
    public const string EventTypeName = "TodoCreatedIntegrationEvent";
}
