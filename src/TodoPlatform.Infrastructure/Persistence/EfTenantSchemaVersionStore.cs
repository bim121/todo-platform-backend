using Microsoft.EntityFrameworkCore;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Domain.Tenancy;

namespace TodoPlatform.Infrastructure.Persistence;

public sealed class EfTenantSchemaVersionStore(AppDbContext db) : ITenantSchemaVersionStore
{
    public async Task<TenantSchemaVersionState?> GetAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var row = await db.TenantSchemaVersions.AsNoTracking()
            .FirstOrDefaultAsync(v => v.TenantId == tenantId, cancellationToken);
        return row is null ? null : ToState(row);
    }

    public async Task<TenantSchemaVersionState?> GetForUpdateAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var row = await LockRowAsync(tenantId, cancellationToken);
        return row is null ? null : ToState(row);
    }

    internal Task<Domain.Entities.TenantSchemaVersion?> LockRowAsync(
        Guid tenantId,
        CancellationToken cancellationToken) =>
        db.Database.IsRelational()
            ? db.TenantSchemaVersions
                .FromSqlInterpolated(
                    $"""
                     SELECT "TenantId", "Track", "CurrentVersion", "UpdatedAt"
                     FROM tenant_schema_versions
                     WHERE "TenantId" = {tenantId}
                     FOR UPDATE
                     """)
                .SingleOrDefaultAsync(cancellationToken)
            : db.TenantSchemaVersions
                .SingleOrDefaultAsync(v => v.TenantId == tenantId, cancellationToken);

    private static TenantSchemaVersionState ToState(Domain.Entities.TenantSchemaVersion row) =>
        new(
            row.TenantId,
            string.IsNullOrWhiteSpace(row.Track) ? MigrationTracks.Stable : row.Track,
            row.CurrentVersion,
            row.UpdatedAt);
}
