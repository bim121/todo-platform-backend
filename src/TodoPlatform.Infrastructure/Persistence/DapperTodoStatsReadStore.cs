using System.Data;
using Dapper;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Infrastructure.Persistence;

public sealed class DapperTodoStatsReadStore(IReadDbConnection readDb) : ITodoStatsReadStore
{
    private static readonly string Sql = SqlResourceLoader.Load("todo-stats.sql");

    public async Task<TodoStatsDto> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        using var connection = readDb.CreateConnection();
        if (connection.State != ConnectionState.Open)
            connection.Open();

        var row = await connection.QuerySingleOrDefaultAsync<TodoStatsDto>(
            new CommandDefinition(
                Sql,
                new { UserId = userId },
                cancellationToken: cancellationToken));

        // View has no row when the user has zero todos.
        return row ?? new TodoStatsDto(userId, 0, 0, 0);
    }
}
