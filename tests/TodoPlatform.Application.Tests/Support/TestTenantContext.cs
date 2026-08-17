using TodoPlatform.Application.Tenancy;
using TodoPlatform.Domain.Tenancy;

namespace TodoPlatform.Application.Tests.Support;

internal sealed class TestTenantContext(
    Guid tenantId,
    string slug = WellKnownTenants.DefaultSlug) : ITenantContext
{
    public static TestTenantContext Default { get; } =
        new(WellKnownTenants.DefaultId, WellKnownTenants.DefaultSlug);

    public Guid TenantId { get; } = tenantId;

    public string? Slug { get; } = slug;

    public bool IsResolved => TenantId != Guid.Empty;

    public void Set(Guid id, string newSlug)
    {
        // Test double is immutable.
    }
}
