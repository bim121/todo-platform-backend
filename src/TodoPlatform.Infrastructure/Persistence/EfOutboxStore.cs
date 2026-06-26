using System.Text.Json;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Domain.Common;

namespace TodoPlatform.Infrastructure.Persistence;

public sealed class EfOutboxStore(AppDbContext db) : IOutboxStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Stage(IEnumerable<IDomainEvent> domainEvents)
    {
        foreach (var domainEvent in domainEvents)
        {
            var eventType = domainEvent.GetType();
            db.OutboxMessages.Add(new OutboxMessage
            {
                Type = eventType.FullName ?? eventType.Name,
                Payload = JsonSerializer.Serialize(domainEvent, eventType, SerializerOptions),
                CreatedAt = domainEvent.OccurredOn
            });
        }
    }
}
