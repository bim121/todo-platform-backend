using TodoPlatform.Application.Exceptions;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Domain.Entities;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Infrastructure.Migrations;

/// <summary>
/// B-12.5 week 2 — lock tenant row, apply one pending step as a <b>logical</b> bump + history.
/// Does not run FluentMigrator on <c>public</c> (beta stays tagged). Physical DDL: B-12.12.
/// </summary>
public sealed class LogicalTenantMigrationRunner(
    AppDbContext db,
    EfTenantSchemaVersionStore versions,
    IMigrationPlanService plans) : ITenantMigrationRunner
{
    public async Task<TenantMigrationApplyResult> ApplyAsync(
        Guid tenantId,
        long? targetVersion,
        string appliedBy,
        CancellationToken cancellationToken = default)
    {
        var row = await versions.LockRowAsync(tenantId, cancellationToken);
        if (row is null)
            throw new NotFoundException($"Schema version for tenant '{tenantId}' was not found.");

        var pending = plans.GetPending(row.Track, row.CurrentVersion);
        if (pending.Count == 0)
            throw new ConflictException(
                $"Tenant '{tenantId}' has no pending migrations on track '{row.Track}'.");

        var next = pending[0];
        if (targetVersion is long target && target != next.Version)
        {
            throw new ConflictException(
                $"Next pending migration is {next.SchemaVersionLabel} (version {next.Version}); "
                + $"cannot skip to {target}. Apply one step at a time.");
        }

        // Week 4: ITenantMigrationRunner runs FluentMigrator inside tenant_* here.
        // Week 2: platform MigrateUp must not apply beta/tenant-stream to public (already tagged).

        row.ApplyVersion(next.Version);
        db.MigrationHistory.Add(
            MigrationHistoryEntry.Record(tenantId, next.SchemaVersionLabel, appliedBy));

        return new TenantMigrationApplyResult(next.Version, next.SchemaVersionLabel, row.Track);
    }
}
