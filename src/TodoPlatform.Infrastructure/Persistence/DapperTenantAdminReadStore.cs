using System.Data;
using System.Text;
using Dapper;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Infrastructure.Tenancy;

namespace TodoPlatform.Infrastructure.Persistence;

public sealed class DapperTenantAdminReadStore(
    IReadDbConnection readDb,
    IMigrationPlanService plans) : ITenantAdminReadStore
{
    public async Task<PagedResult<TenantAdminDto>> ListAsync(
        TenantAdminListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var built = TenantAdminListSqlBuilder.Build(filter);

        using var connection = OpenWithBypass();
        var total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(built.CountSql, built.Parameters, cancellationToken: cancellationToken));

        var rows = await connection.QueryAsync<TenantAdminRow>(
            new CommandDefinition(built.PageSql, built.Parameters, cancellationToken: cancellationToken));

        var items = rows.Select(Map).ToList();
        return new PagedResult<TenantAdminDto>(items, total, filter.Skip, filter.Take);
    }

    public async Task<TenantAdminDto?> GetByIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        using var connection = OpenWithBypass();
        var row = await connection.QuerySingleOrDefaultAsync<TenantAdminRow>(
            new CommandDefinition(
                SqlResourceLoader.Load("admin-tenant-by-id.sql"),
                new { TenantId = tenantId },
                cancellationToken: cancellationToken));
        return row is null ? null : Map(row);
    }

    private IDbConnection OpenWithBypass()
    {
        var connection = readDb.CreateConnection();
        if (connection.State != ConnectionState.Open)
            connection.Open();

        TenantSession.ApplyBypass(connection);
        return connection;
    }

    private TenantAdminDto Map(TenantAdminRow row) =>
        TenantAdminMapper.ToDto(row.Id, row.Name, row.CurrentVersion, row.Track, row.Status, plans);

    private sealed class TenantAdminRow
    {
        public string Id { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public long CurrentVersion { get; init; }

        public string Track { get; init; } = string.Empty;

        public string Status { get; init; } = string.Empty;
    }
}

/// <summary>Parameterized admin tenant list COUNT + page (B-12.4).</summary>
internal static class TenantAdminListSqlBuilder
{
    private const string FromJoin = """
        FROM tenants t
        LEFT JOIN tenant_schema_versions v ON v."TenantId" = t."Id"
        """;

    private const string SelectColumns = """
        t."Id"::text AS Id,
        t."Name" AS Name,
        COALESCE(v."CurrentVersion", 0) AS CurrentVersion,
        COALESCE(v."Track", 'stable') AS Track,
        LOWER(t."Status") AS Status
        """;

    public sealed record BuiltQuery(string CountSql, string PageSql, DynamicParameters Parameters);

    public static BuiltQuery Build(TenantAdminListFilter filter)
    {
        var where = new StringBuilder("WHERE 1=1");
        var parameters = new DynamicParameters();
        parameters.Add("Skip", filter.Skip);
        parameters.Add("Take", filter.Take);

        if (!string.IsNullOrWhiteSpace(filter.Track))
        {
            where.Append(""" AND LOWER(COALESCE(v."Track", 'stable')) = LOWER(@Track)""");
            parameters.Add("Track", filter.Track.Trim());
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            where.Append(""" AND LOWER(t."Status"::text) = LOWER(@Status)""");
            parameters.Add("Status", filter.Status.Trim());
        }

        var whereSql = where.ToString();
        var countSql = $"""SELECT COUNT(*)::int {FromJoin} {whereSql};""";
        var pageSql = $"""
            SELECT {SelectColumns}
            {FromJoin}
            {whereSql}
            ORDER BY t."Name"
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
            """;

        return new BuiltQuery(countSql, pageSql, parameters);
    }
}
