namespace TodoPlatform.Application.Interfaces;

/// <summary>Creates tenant PostgreSQL schema and applies baseline tenant-stream (B-12.13).</summary>
public interface ITenantSchemaProvisioner
{
    Task ProvisionAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task EnsureAllTenantsProvisionedAsync(CancellationToken cancellationToken = default);
}
