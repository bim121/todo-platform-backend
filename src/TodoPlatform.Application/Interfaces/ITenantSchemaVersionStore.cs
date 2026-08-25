namespace TodoPlatform.Application.Interfaces;

public interface ITenantSchemaVersionStore
{
    Task<TenantSchemaVersionState?> GetAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Load row with <c>FOR UPDATE</c> when on Postgres (same transaction as apply).
    /// </summary>
    Task<TenantSchemaVersionState?> GetForUpdateAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

public sealed record TenantSchemaVersionState(
    Guid TenantId,
    string Track,
    long CurrentVersion,
    DateTimeOffset UpdatedAt);
