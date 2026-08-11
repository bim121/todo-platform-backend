using System.Data;
using Dapper;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Infrastructure.Persistence;

public sealed class DapperTodoFilterReadStore(IReadDbConnection readDb) : ITodoFilterReadStore
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
        var built = TodoFilterSqlBuilder.Build(userId, status, priority, completed, search, skip, take);

        using var connection = readDb.CreateConnection();
        if (connection.State != ConnectionState.Open)
            connection.Open();

        var total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(built.CountSql, built.Parameters, cancellationToken: cancellationToken));

        var items = (await connection.QueryAsync<TodoListItemDto>(
            new CommandDefinition(built.PageSql, built.Parameters, cancellationToken: cancellationToken)))
            .AsList();

        return new PagedResult<TodoListItemDto>(items, total, skip, take);
    }
}
