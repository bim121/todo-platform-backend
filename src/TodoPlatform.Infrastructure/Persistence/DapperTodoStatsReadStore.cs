using System.Data;
using System.Reflection;
using Dapper;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Infrastructure.Persistence;

public sealed class DapperTodoStatsReadStore(IReadDbConnection readDb) : ITodoStatsReadStore
{
    private static readonly string Sql = LoadEmbeddedSql("todo-stats.sql");

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

    private static string LoadEmbeddedSql(string fileName)
    {
        var assembly = typeof(DapperTodoStatsReadStore).Assembly;
        var resourceName = assembly
            .GetManifestResourceNames()
            .Single(n => n.EndsWith($".{fileName}", StringComparison.OrdinalIgnoreCase)
                || n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded SQL resource '{fileName}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
