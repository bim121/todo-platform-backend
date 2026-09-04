namespace TodoPlatform.Application.IntegrationEvents;

public sealed record TodoUpdatedIntegrationEvent(
    Guid TodoId,
    Guid UserId,
    Guid TenantId,
    string Title,
    bool Completed,
    DateTimeOffset OccurredOn) : IIntegrationEvent
{
    public const string EventTypeName = "TodoUpdatedIntegrationEvent";
}
