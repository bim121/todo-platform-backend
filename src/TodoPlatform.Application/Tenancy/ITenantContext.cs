namespace TodoPlatform.Application.Tenancy;

/// <summary>
/// Request-scoped tenant resolved by <c>TenantResolutionMiddleware</c> (header or JWT claim).
/// </summary>
public interface ITenantContext
{
    Guid TenantId { get; }

    string? Slug { get; }

    /// <summary>PostgreSQL schema for tenant-owned objects (B-12.11).</summary>
    string? SchemaName { get; }

    bool IsResolved { get; }

    void Set(Guid tenantId, string slug, string? schemaName = null);
}
