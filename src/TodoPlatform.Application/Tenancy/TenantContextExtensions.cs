using TodoPlatform.Application.Exceptions;

namespace TodoPlatform.Application.Tenancy;

public static class TenantContextExtensions
{
    /// <summary>
    /// Tenant id resolved by middleware. Never taken from the request body.
    /// </summary>
    public static Guid RequireTenantId(this ITenantContext tenantContext)
    {
        ArgumentNullException.ThrowIfNull(tenantContext);

        if (!tenantContext.IsResolved || tenantContext.TenantId == Guid.Empty)
        {
            throw ValidationException.ForField(
                "X-Tenant-Id",
                "Tenant must be resolved from header 'X-Tenant-Id' (UUID or slug) or JWT claim 'tenant_id'.");
        }

        return tenantContext.TenantId;
    }
}
