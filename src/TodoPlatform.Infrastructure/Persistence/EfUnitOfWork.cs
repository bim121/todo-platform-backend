using Microsoft.EntityFrameworkCore;
using TodoPlatform.Application.Common;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Domain.Common;
using TodoPlatform.Infrastructure.Repositories;

namespace TodoPlatform.Infrastructure.Persistence;

public sealed class EfUnitOfWork(
    AppDbContext db,
    IDomainEventDispatcher dispatcher,
    IOutboxStore outboxStore) : IUnitOfWork
{
    private readonly Dictionary<Type, object> _repositories = [];

    public IRepository<T> Repository<T>() where T : Entity
    {
        var type = typeof(T);
        if (!_repositories.TryGetValue(type, out var repository))
        {
            repository = new EfRepository<T>(db);
            _repositories[type] = repository;
        }

        return (IRepository<T>)repository;
    }

    public void Add<T>(T entity) where T : Entity =>
        db.Set<T>().Add(entity);

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        var pending = db.ChangeTracker.Entries<Entity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .Select(e => (Entity: e, Events: e.DomainEvents.ToList()))
            .ToList();

        var allEvents = pending.SelectMany(p => p.Events).ToList();
        if (allEvents.Count > 0)
            outboxStore.Stage(allEvents);

        await db.SaveChangesAsync(cancellationToken);

        foreach (var (entity, _) in pending)
            entity.ClearDomainEvents();

        if (allEvents.Count > 0)
            await dispatcher.DispatchEventsAsync(allEvents, cancellationToken);
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in db.ChangeTracker.Entries<Entity>())
            entry.Entity.ClearDomainEvents();

        db.ChangeTracker.Clear();
        return Task.CompletedTask;
    }
}
