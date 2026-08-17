using System.Data;
using Dapper;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Infrastructure.Tenancy;

namespace TodoPlatform.Infrastructure.Persistence;

public sealed class DapperSystemStatsReadStore(IReadDbConnection readDb) : ISystemStatsReadStore
{
    private static readonly string Sql = SqlResourceLoader.Load("system-stats.sql");

    public async Task<SystemStatsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        using var connection = readDb.CreateConnection();
        if (connection.State != ConnectionState.Open)
            connection.Open();

        // Platform-wide admin aggregate — bypass tenant RLS for this query only.
        TenantSession.ApplyBypass(connection);
        try
        {
            return await connection.QuerySingleAsync<SystemStatsDto>(
                new CommandDefinition(Sql, cancellationToken: cancellationToken));
        }
        finally
        {
            TenantSession.Reset(connection);
        }
    }
}
