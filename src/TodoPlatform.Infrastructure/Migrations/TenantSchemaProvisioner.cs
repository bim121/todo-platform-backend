using Microsoft.EntityFrameworkCore;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Domain.Tenancy;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Infrastructure.Migrations;

/// <summary>
/// B-12.13 — CREATE SCHEMA + tenant-stream baseline for new tenants (stable only).
/// </summary>
public sealed class TenantSchemaProvisioner(
    AppDbContext db,
    ITenantFluentMigrator tenantMigrator) : ITenantSchemaProvisioner
{
    public async Task ProvisionAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (!db.Database.IsRelational())
            return;

        var tenant = await db.Tenants.SingleOrDefaultAsync(t => t.Id == tenantId, cancellationToken)
            ?? throw new InvalidOperationException($"Tenant '{tenantId}' was not found.");

        var schemaName = string.IsNullOrWhiteSpace(tenant.SchemaName)
            ? TenantSchemaNaming.FromSlug(tenant.Slug)
            : tenant.SchemaName;

        if (string.IsNullOrWhiteSpace(tenant.SchemaName))
        {
            tenant.AssignSchemaName(schemaName);
            await db.SaveChangesAsync(cancellationToken);
        }

        await db.Database.ExecuteSqlRawAsync(
            $"""CREATE SCHEMA IF NOT EXISTS "{schemaName}";""",
            cancellationToken);

        tenantMigrator.MigrateUp(schemaName, TenantPhysicalMigrationVersions.Baseline);
    }

    public async Task EnsureAllTenantsProvisionedAsync(CancellationToken cancellationToken = default)
    {
        if (!db.Database.IsRelational())
            return;

        var tenantIds = await db.Tenants.Select(t => t.Id).ToListAsync(cancellationToken);
        foreach (var tenantId in tenantIds)
            await ProvisionAsync(tenantId, cancellationToken);
    }
}
