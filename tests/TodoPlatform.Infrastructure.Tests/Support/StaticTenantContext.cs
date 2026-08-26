using TodoPlatform.Application.Tenancy;
using TodoPlatform.Domain.Tenancy;

namespace TodoPlatform.Infrastructure.Tests.Support;

internal sealed class StaticTenantContext(Guid tenantId, string slug = WellKnownTenants.DefaultSlug)
    : ITenantContext
{
    public Guid TenantId { get; } = tenantId;

    public string? Slug { get; } = slug;

    public string? SchemaName { get; } = TenantSchemaNaming.FromSlug(slug);

    public bool IsResolved => TenantId != Guid.Empty;

    public void Set(Guid id, string newSlug, string? schemaName = null)
    {
        // Test double is immutable.
    }
}
