using System.Data;
using Dapper;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Infrastructure.Tenancy;

namespace TodoPlatform.Infrastructure.Persistence;

public sealed class DapperTenantAdminReadStore(
    IReadDbConnection readDb,
    IMigrationPlanService plans) : ITenantAdminReadStore
{
    private static readonly string ListSql = SqlResourceLoader.Load("admin-tenants.sql");
    private static readonly string ByIdSql = SqlResourceLoader.Load("admin-tenant-by-id.sql");

    public async Task<IReadOnlyList<TenantAdminDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        using var connection = OpenWithBypass();
        var rows = await connection.QueryAsync<TenantAdminRow>(
            new CommandDefinition(ListSql, cancellationToken: cancellationToken));
        return rows.Select(Map).ToList();
    }

    public async Task<TenantAdminDto?> GetByIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        using var connection = OpenWithBypass();
        var row = await connection.QuerySingleOrDefaultAsync<TenantAdminRow>(
            new CommandDefinition(
                ByIdSql,
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
