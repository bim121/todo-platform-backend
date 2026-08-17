using Microsoft.EntityFrameworkCore;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Infrastructure.Persistence;

/// <summary>In-memory / test fallback when Postgres is unavailable.</summary>
public sealed class EfSystemStatsReadStore(AppDbContext db) : ISystemStatsReadStore
{
    public async Task<SystemStatsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var totalUsers = await db.Users.IgnoreQueryFilters().AsNoTracking().CountAsync(cancellationToken);
        var totalTodos = await db.Todos.IgnoreQueryFilters().AsNoTracking().CountAsync(cancellationToken);
        var avg = totalUsers == 0
            ? 0m
            : Math.Round((decimal)totalTodos / totalUsers, 2, MidpointRounding.AwayFromZero);

        return new SystemStatsDto(totalUsers, totalTodos, avg);
    }
}
