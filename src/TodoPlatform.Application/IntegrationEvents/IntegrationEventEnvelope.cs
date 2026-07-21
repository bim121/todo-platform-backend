namespace TodoPlatform.Application.IntegrationEvents;

/// <summary>
/// Envelope stored in <c>outbox_messages.payload</c> for versioned async publishing (B-07).
/// Shape: <c>{ "type": "...", "version": 1, "data": { ... } }</c>.
/// </summary>
public sealed record IntegrationEventEnvelope(
    string Type,
    int Version,
    object Data,
    DateTimeOffset OccurredOn)
{
    public const int CurrentVersion = 1;
}
