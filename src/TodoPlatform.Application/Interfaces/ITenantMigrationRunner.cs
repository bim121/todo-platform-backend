namespace TodoPlatform.Application.Interfaces;

/// <summary>
/// Applies one pending migration step for a tenant (B-12.5).
/// Week 2: logical bump + history (no public FluentMigrator for beta).
/// Week 4: DDL inside <c>tenant_*</c> schema.
/// </summary>
public interface ITenantMigrationRunner
{
    Task<TenantMigrationApplyResult> ApplyAsync(
        Guid tenantId,
        long? targetVersion,
        string appliedBy,
        CancellationToken cancellationToken = default);
}

public sealed record TenantMigrationApplyResult(
    long AppliedVersion,
    string SchemaVersionLabel,
    string Track);
