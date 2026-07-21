using System.Text.Json;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Domain.Common;

namespace TodoPlatform.Infrastructure.Persistence;

public sealed class EfOutboxStore(
    AppDbContext db,
    IDomainEventToIntegrationEventMapper mapper) : IOutboxStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Stage(IEnumerable<IDomainEvent> domainEvents)
    {
        foreach (var domainEvent in domainEvents)
        {
            var envelope = mapper.Map(domainEvent);
            if (envelope is null)
                continue;

            db.OutboxMessages.Add(new OutboxMessage
            {
                Type = envelope.Type,
                Payload = JsonSerializer.Serialize(envelope, SerializerOptions),
                CreatedAt = envelope.OccurredOn
            });
        }
    }
}
