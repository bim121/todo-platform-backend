using TodoPlatform.Application.Migrations;

namespace TodoPlatform.Application.Interfaces;

/// <summary>
/// Pre-apply checks before a tenant migration step runs (B-12.7).
/// Beta-tagged migrations simulate breaking/incompatible DDL.
/// </summary>
public interface ITenantMigrationCompatibilityValidator
{
    Task ValidateAsync(
        Guid tenantId,
        string track,
        MigrationInfo nextMigration,
        CancellationToken cancellationToken = default);
}
