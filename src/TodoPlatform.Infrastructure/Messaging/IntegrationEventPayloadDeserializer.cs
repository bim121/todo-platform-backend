using System.Text.Json;
using TodoPlatform.Application.IntegrationEvents;

namespace TodoPlatform.Infrastructure.Messaging;

public static class IntegrationEventPayloadDeserializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static IIntegrationEvent? Deserialize(string type, string payload)
    {
        using var document = JsonDocument.Parse(payload);
        if (!document.RootElement.TryGetProperty("data", out var dataElement))
            return null;

        return type switch
        {
            TodoCreatedIntegrationEvent.EventTypeName =>
                dataElement.Deserialize<TodoCreatedIntegrationEvent>(SerializerOptions),
            TodoUpdatedIntegrationEvent.EventTypeName =>
                dataElement.Deserialize<TodoUpdatedIntegrationEvent>(SerializerOptions),
            TodoDeletedIntegrationEvent.EventTypeName =>
                dataElement.Deserialize<TodoDeletedIntegrationEvent>(SerializerOptions),
            TodoCompletedIntegrationEvent.EventTypeName =>
                dataElement.Deserialize<TodoCompletedIntegrationEvent>(SerializerOptions),
            TenantMigrationAppliedIntegrationEvent.EventTypeName =>
                dataElement.Deserialize<TenantMigrationAppliedIntegrationEvent>(SerializerOptions),
            _ => null
        };
    }
}
