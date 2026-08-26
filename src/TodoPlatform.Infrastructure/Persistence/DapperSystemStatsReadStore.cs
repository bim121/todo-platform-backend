using System.Data;
using Dapper;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Domain.Tenancy;
using TodoPlatform.Infrastructure.Tenancy;

namespace TodoPlatform.Infrastructure.Persistence;

public sealed class DapperSystemStatsReadStore(IReadDbConnection readDb) : ISystemStatsReadStore
{
    private static readonly string LegacySql = SqlResourceLoader.Load("system-stats.sql");

    public async Task<SystemStatsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        using var connection = readDb.CreateConnection();
        if (connection.State != ConnectionState.Open)
            connection.Open();

        // Platform-wide admin aggregate — bypass tenant RLS and search_path (B-12.11).
        TenantSession.ApplyBypass(connection);
        try
        {
            var schemas = (await connection.QueryAsync<string>(
                new CommandDefinition(
                    """
                    SELECT "SchemaName"
                    FROM public.tenants
                    WHERE "SchemaName" IS NOT NULL AND "SchemaName" <> ''
                    """,
                    cancellationToken: cancellationToken))).ToArray();

            if (schemas.Length == 0)
            {
                return await connection.QuerySingleAsync<SystemStatsDto>(
                    new CommandDefinition(LegacySql, cancellationToken: cancellationToken));
            }

            var union = string.Join(
                " UNION ALL ",
                schemas
                    .Where(TenantSchemaNaming.IsValidSchemaName)
                    .Select(schema =>
                        $"""
                        SELECT u."Id" AS user_id, t."Id" AS todo_id
                        FROM "{schema}".users u
                        LEFT JOIN "{schema}".todos t ON t."UserId" = u."Id"
                        """));

            var sql = $"""
                SELECT
                    COUNT(DISTINCT x.user_id)::int AS "TotalUsers",
                    COUNT(x.todo_id)::int AS "TotalTodos",
                    COALESCE(
                        ROUND(COUNT(x.todo_id)::numeric / NULLIF(COUNT(DISTINCT x.user_id), 0), 2),
                        0
                    ) AS "AvgTodosPerUser"
                FROM ({union}) x;
                """;

            return await connection.QuerySingleAsync<SystemStatsDto>(
                new CommandDefinition(sql, cancellationToken: cancellationToken));
        }
        finally
        {
            TenantSession.Reset(connection);
        }
    }
}
