using TodoPlatform.Domain.Entities;

namespace TodoPlatform.Application.Interfaces;

public interface ITodoRepository
{
    Task<IReadOnlyList<Todo>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Todo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Todo> AddAsync(Todo todo, CancellationToken cancellationToken = default);

    Task UpdateAsync(Todo todo, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
