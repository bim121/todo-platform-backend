using TodoPlatform.Application.Dtos;
using TodoPlatform.Domain.Entities;
using TodoPlatform.Domain.Specifications;

namespace TodoPlatform.Application.Interfaces;

/// <summary>
/// Todo persistence. List queries use <see cref="Specification{T}"/> via <see cref="ListAsync"/> (B-04).
/// Ad-hoc methods such as GetByUserId/GetActive were removed in B-04.6.
/// </summary>
public interface ITodoRepository
{
    /// <summary>B-04: list via <see cref="Specification{T}"/> (e.g. <c>TodoByUserSpecification</c>).</summary>
    Task<IReadOnlyList<Todo>> ListAsync(
        Specification<Todo> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// B-09.4: project to <see cref="TodoDto"/> in SQL (no full entity materialization, no Include).
    /// </summary>
    Task<IReadOnlyList<TodoDto>> ListDtosAsync(
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
