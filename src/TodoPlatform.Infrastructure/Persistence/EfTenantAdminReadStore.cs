using Microsoft.EntityFrameworkCore;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Domain.Tenancy;

namespace TodoPlatform.Infrastructure.Persistence;

/// <summary>In-memory / test fallback when Postgres (and Dapper SQL) is unavailable.</summary>
public sealed class EfTenantAdminReadStore(
    AppDbContext db,
    IMigrationPlanService plans) : ITenantAdminReadStore
{
    public async Task<IReadOnlyList<TenantAdminDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await db.Tenants.AsNoTracking().OrderBy(t => t.Name).ToListAsync(cancellationToken);
        var versions = await db.TenantSchemaVersions.AsNoTracking()
            .ToDictionaryAsync(v => v.TenantId, cancellationToken);

        return tenants.Select(t => Map(t.Id, t.Name, t.Status.ToString(), versions)).ToList();
    }

    public async Task<TenantAdminDto?> GetByIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant is null)
            return null;

        var versions = await db.TenantSchemaVersions.AsNoTracking()
            .ToDictionaryAsync(v => v.TenantId, cancellationToken);

        return Map(tenant.Id, tenant.Name, tenant.Status.ToString(), versions);
    }

    private TenantAdminDto Map(
        Guid id,
        string name,
        string status,
        IReadOnlyDictionary<Guid, Domain.Entities.TenantSchemaVersion> versions)
    {
        versions.TryGetValue(id, out var version);
        return TenantAdminMapper.ToDto(
            id.ToString(),
            name,
            version?.CurrentVersion ?? 0,
            version?.Track ?? MigrationTracks.Stable,
            status,
            plans);
    }
}
