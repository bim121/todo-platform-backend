namespace TodoPlatform.Application.Tenancy;

/// <summary>
/// Request-scoped tenant resolved by <c>TenantResolutionMiddleware</c> (header or JWT claim).
/// </summary>
public interface ITenantContext
{
    Guid TenantId { get; }

    string? Slug { get; }

    bool IsResolved { get; }

    void Set(Guid tenantId, string slug);
}
