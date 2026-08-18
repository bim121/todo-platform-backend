using TodoPlatform.Application.Migrations;

namespace TodoPlatform.Application.Interfaces;

/// <summary>
/// Pending FluentMigrator versions for a tenant's stable/beta track (B-12.2).
/// Shared-schema DDL is global; this catalog is the logical per-tenant plan.
/// </summary>
public interface IMigrationPlanService
{
    IReadOnlyList<MigrationInfo> Catalog { get; }

    long LatestStableVersion { get; }

    MigrationInfo? Find(long version);

    IReadOnlyList<MigrationInfo> GetPending(string track, long currentVersion);
}
