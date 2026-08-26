using TodoPlatform.Domain.Tenancy;

namespace TodoPlatform.Application.Tenancy;

public sealed class TenantContext : ITenantContext
{
    public Guid TenantId { get; private set; }

    public string? Slug { get; private set; }

    public string? SchemaName { get; private set; }

    public bool IsResolved { get; private set; }

    public void Set(Guid tenantId, string slug, string? schemaName = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug is required.", nameof(slug));

        TenantId = tenantId;
        Slug = slug;
        SchemaName = string.IsNullOrWhiteSpace(schemaName)
            ? TenantSchemaNaming.FromSlug(slug)
            : schemaName;
        IsResolved = true;
    }
}
