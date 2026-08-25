namespace TodoPlatform.Application.IntegrationEvents;

public sealed record TenantMigrationAppliedIntegrationEvent(
    Guid TenantId,
    string Version,
    string AppliedBy,
    DateTimeOffset OccurredOn) : IIntegrationEvent
{
    public const string EventTypeName = "TenantMigrationAppliedIntegrationEvent";
}
