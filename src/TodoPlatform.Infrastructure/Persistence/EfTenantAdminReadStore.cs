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
    public async Task<PagedResult<TenantAdminDto>> ListAsync(
        TenantAdminListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var tenants = await db.Tenants.AsNoTracking().ToListAsync(cancellationToken);
        var versions = await db.TenantSchemaVersions.AsNoTracking()
            .ToDictionaryAsync(v => v.TenantId, cancellationToken);

        IEnumerable<(Domain.Entities.Tenant Tenant, Domain.Entities.TenantSchemaVersion? Version)> joined =
            tenants.Select(t =>
            {
                versions.TryGetValue(t.Id, out var version);
                return (t, version);
            });

        if (!string.IsNullOrWhiteSpace(filter.Track))
        {
            joined = joined.Where(x =>
                string.Equals(
                    x.Version?.Track ?? MigrationTracks.Stable,
                    filter.Track.Trim(),
                    StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            joined = joined.Where(x =>
                string.Equals(
                    x.Tenant.Status.ToString(),
                    filter.Status.Trim(),
                    StringComparison.OrdinalIgnoreCase));
        }

        var filtered = joined.OrderBy(x => x.Tenant.Name).ToList();
        var total = filtered.Count;
        var page = filtered
            .Skip(filter.Skip)
            .Take(filter.Take)
            .Select(x => Map(x.Tenant.Id, x.Tenant.Name, x.Tenant.Status.ToString(), x.Version))
            .ToList();

        return new PagedResult<TenantAdminDto>(page, total, filter.Skip, filter.Take);
    }

    public async Task<TenantAdminDto?> GetByIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant is null)
            return null;

        var version = await db.TenantSchemaVersions.AsNoTracking()
            .FirstOrDefaultAsync(v => v.TenantId == tenantId, cancellationToken);

        return Map(tenant.Id, tenant.Name, tenant.Status.ToString(), version);
    }

    private TenantAdminDto Map(
        Guid id,
        string name,
        string status,
        Domain.Entities.TenantSchemaVersion? version) =>
        TenantAdminMapper.ToDto(
            id.ToString(),
            name,
            version?.CurrentVersion ?? 0,
            version?.Track ?? MigrationTracks.Stable,
            status,
            plans);
}
