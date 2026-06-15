using Microsoft.EntityFrameworkCore;
using TodoPlatform.Application.Common;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Domain.Common;

namespace TodoPlatform.Infrastructure.Persistence;

public sealed class EfUnitOfWork(AppDbContext db, IDomainEventDispatcher dispatcher) : IUnitOfWork
{
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        var pending = db.ChangeTracker.Entries<Entity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .Select(e => (Entity: e, Events: e.DomainEvents.ToList()))
            .ToList();

        await db.SaveChangesAsync(cancellationToken);

        foreach (var (entity, _) in pending)
            entity.ClearDomainEvents();

        var allEvents = pending.SelectMany(p => p.Events).ToList();
        if (allEvents.Count > 0)
            await dispatcher.DispatchEventsAsync(allEvents, cancellationToken);
    }
}
