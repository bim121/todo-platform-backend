using TodoPlatform.Domain.Common;

namespace TodoPlatform.Application.Interfaces;

public interface IRepository<T> where T : Entity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(T entity);

    void Update(T entity);

    void Remove(T entity);
}
