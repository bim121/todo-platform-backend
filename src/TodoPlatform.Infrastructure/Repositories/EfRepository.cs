using Microsoft.EntityFrameworkCore;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Domain.Common;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Infrastructure.Repositories;

public sealed class EfRepository<T>(AppDbContext db) : IRepository<T> where T : Entity
{
    public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Set<T>().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public void Add(T entity) =>
        db.Set<T>().Add(entity);

    public void Update(T entity) =>
        db.Set<T>().Update(entity);

    public void Remove(T entity) =>
        db.Set<T>().Remove(entity);
}
