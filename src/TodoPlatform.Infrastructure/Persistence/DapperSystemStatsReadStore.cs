using System.Data;
using Dapper;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Infrastructure.Persistence;

public sealed class DapperSystemStatsReadStore(IReadDbConnection readDb) : ISystemStatsReadStore
{
    private static readonly string Sql = SqlResourceLoader.Load("system-stats.sql");

    public async Task<SystemStatsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        using var connection = readDb.CreateConnection();
        if (connection.State != ConnectionState.Open)
            connection.Open();

        return await connection.QuerySingleAsync<SystemStatsDto>(
            new CommandDefinition(Sql, cancellationToken: cancellationToken));
    }
}
