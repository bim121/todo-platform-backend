using TodoPlatform.Domain.Common;

namespace TodoPlatform.Application.Interfaces;

public interface IUnitOfWork
{
    IRepository<T> Repository<T>() where T : Entity;

    void Add<T>(T entity) where T : Entity;

    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}
