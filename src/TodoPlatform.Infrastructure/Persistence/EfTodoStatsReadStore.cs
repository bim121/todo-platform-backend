using Microsoft.EntityFrameworkCore;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Infrastructure.Persistence;

/// <summary>
/// In-memory / test fallback when Postgres (and the SQL view) is unavailable.
/// Production read path uses <see cref="DapperTodoStatsReadStore"/>.
/// </summary>
public sealed class EfTodoStatsReadStore(AppDbContext db) : ITodoStatsReadStore
{
    public async Task<TodoStatsDto> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var todos = await db.Todos
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .Select(t => t.Completed)
            .ToListAsync(cancellationToken);

        var total = todos.Count;
        var completed = todos.Count(c => c);
        return new TodoStatsDto(userId, total, total - completed, completed);
    }
}
