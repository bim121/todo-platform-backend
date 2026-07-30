using TodoPlatform.Application.Dtos;

namespace TodoPlatform.Application.Interfaces;

/// <summary>
/// Read-model access for per-user todo aggregates (Dapper against a SQL view in production).
/// </summary>
public interface ITodoStatsReadStore
{
    Task<TodoStatsDto> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
