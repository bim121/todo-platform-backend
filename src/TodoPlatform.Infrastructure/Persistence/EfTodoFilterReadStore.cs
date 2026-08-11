using Microsoft.EntityFrameworkCore;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Mapping;
using TodoPlatform.Domain.Enums;

namespace TodoPlatform.Infrastructure.Persistence;

/// <summary>In-memory / test fallback when Postgres is unavailable.</summary>
public sealed class EfTodoFilterReadStore(AppDbContext db) : ITodoFilterReadStore
{
    public async Task<PagedResult<TodoListItemDto>> SearchAsync(
        Guid userId,
        string? status,
        string? priority,
        bool? completed,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = db.Todos.AsNoTracking().Where(t => t.UserId == userId);

        if (!string.IsNullOrWhiteSpace(status)
            && TodoContractMapper.TryParseStatus(status, out var statusEnum))
        {
            query = query.Where(t => t.Status == statusEnum);
        }

        if (!string.IsNullOrWhiteSpace(priority)
            && TodoContractMapper.TryParsePriority(priority, out var priorityEnum))
        {
            query = query.Where(t => t.Priority == priorityEnum);
        }

        if (completed.HasValue)
            query = query.Where(t => t.Completed == completed.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(t => t.Title.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(t => t.Id)
            .Skip(skip)
            .Take(take)
            .Select(t => new TodoListItemDto(
                t.Id,
                t.Title,
                t.Completed,
                t.UserId,
                t.Status == TodoStatus.Todo
                    ? "todo"
                    : t.Status == TodoStatus.InProgress
                        ? "in_progress"
                        : "done",
                t.Priority == TodoPriority.Low
                    ? "low"
                    : t.Priority == TodoPriority.High
                        ? "high"
                        : "medium"))
            .ToListAsync(cancellationToken);

        return new PagedResult<TodoListItemDto>(items, total, skip, take);
    }
}
