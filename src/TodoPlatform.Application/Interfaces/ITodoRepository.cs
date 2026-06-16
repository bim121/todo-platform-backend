using TodoPlatform.Domain.Entities;
using TodoPlatform.Domain.Specifications;

namespace TodoPlatform.Application.Interfaces;

/// <summary>
/// Temporary data access for todos (replaced by MediatR handlers in B-03).
/// Method names align with future Commands/Queries.
/// </summary>
public interface ITodoRepository
{
    /// <summary>B-04: list via <see cref="Specification{T}"/> (e.g. <c>TodoByUserSpecification</c>).</summary>
    Task<IReadOnlyList<Todo>> ListAsync(
        Specification<Todo> specification,
        CancellationToken cancellationToken = default);

    /// <summary>B-03: <c>GetTodoByIdQuery</c></summary>
    Task<Todo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>B-03: <c>CreateTodoCommand</c></summary>
    Task<Todo> AddAsync(Todo todo, CancellationToken cancellationToken = default);

    /// <summary>B-03: <c>UpdateTodoCommand</c></summary>
    Task UpdateAsync(Todo todo, CancellationToken cancellationToken = default);

    /// <summary>B-03: <c>DeleteTodoCommand</c></summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
