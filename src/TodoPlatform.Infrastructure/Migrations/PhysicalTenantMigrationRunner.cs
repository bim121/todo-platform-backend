using Microsoft.EntityFrameworkCore;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Exceptions;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Domain.Entities;
using TodoPlatform.Domain.Tenancy;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Infrastructure.Migrations;

/// <summary>
/// B-12.5 week 2 — lock tenant row, apply one pending step as logical bump + history.
/// B-12.7 — optimistic concurrency, compatibility, preview.
/// B-12.12 week 4 — physical FluentMigrator DDL inside <c>tenant_*</c> on PostgreSQL.
/// </summary>
public sealed class PhysicalTenantMigrationRunner(
    AppDbContext db,
    EfTenantSchemaVersionStore versions,
    IMigrationPlanService plans,
    ITenantMigrationCompatibilityValidator compatibility,
    ITenantFluentMigrator tenantMigrator) : ITenantMigrationRunner
{
    public async Task<MigrationApplyPreviewDto> PreviewAsync(
        Guid tenantId,
        long? targetVersion,
        DateTimeOffset? expectedUpdatedAt = null,
        CancellationToken cancellationToken = default)
    {
        var row = await db.TenantSchemaVersions.AsNoTracking()
            .SingleOrDefaultAsync(v => v.TenantId == tenantId, cancellationToken);

        if (row is null)
            throw new NotFoundException($"Schema version for tenant '{tenantId}' was not found.");

        return await BuildPreviewAsync(row, targetVersion, expectedUpdatedAt, dryRun: true, cancellationToken);
    }

    public async Task<TenantMigrationApplyResult> ApplyAsync(
        Guid tenantId,
        long? targetVersion,
        string appliedBy,
        DateTimeOffset? expectedUpdatedAt = null,
        CancellationToken cancellationToken = default)
    {
        var row = await versions.LockRowAsync(tenantId, cancellationToken)
            ?? throw new NotFoundException($"Schema version for tenant '{tenantId}' was not found.");

        AssertExpectedUpdatedAt(row, expectedUpdatedAt);

        var next = await ResolveNextMigrationAsync(row, targetVersion, cancellationToken);
        await compatibility.ValidateAsync(tenantId, row.Track, next, cancellationToken);

        if (db.Database.IsRelational())
            await ApplyPhysicalAsync(tenantId, next.Version, cancellationToken);

        row.ApplyVersion(next.Version);
        db.MigrationHistory.Add(
            MigrationHistoryEntry.Record(tenantId, next.SchemaVersionLabel, appliedBy));

        return new TenantMigrationApplyResult(next.Version, next.SchemaVersionLabel, row.Track);
    }

    private async Task ApplyPhysicalAsync(
        Guid tenantId,
        long logicalVersion,
        CancellationToken cancellationToken)
    {
        var tenant = await db.Tenants.AsNoTracking()
            .SingleOrDefaultAsync(t => t.Id == tenantId, cancellationToken)
            ?? throw new NotFoundException($"Tenant '{tenantId}' was not found.");

        var schemaName = string.IsNullOrWhiteSpace(tenant.SchemaName)
            ? TenantSchemaNaming.FromSlug(tenant.Slug)
            : tenant.SchemaName;

        var physicalVersion = TenantPhysicalMigrationVersions.ForLogical(logicalVersion);
        tenantMigrator.MigrateUp(schemaName, physicalVersion);
    }

    private async Task<MigrationApplyPreviewDto> BuildPreviewAsync(
        TenantSchemaVersion row,
        long? targetVersion,
        DateTimeOffset? expectedUpdatedAt,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        AssertExpectedUpdatedAt(row, expectedUpdatedAt);

        var pending = plans.GetPending(row.Track, row.CurrentVersion);
        var currentLabel = plans.Find(row.CurrentVersion)?.SchemaVersionLabel ?? $"V{row.CurrentVersion:D3}";

        if (pending.Count == 0)
        {
            return new MigrationApplyPreviewDto(
                DryRun: dryRun,
                CurrentVersion: currentLabel,
                Track: row.Track,
                WouldApply: null);
        }

        var next = pending[0];
        if (targetVersion is long target && target != next.Version)
        {
            throw new ConflictException(
                $"Next pending migration is {next.SchemaVersionLabel} (version {next.Version}); "
                + $"cannot skip to {target}. Apply one step at a time.");
        }

        await compatibility.ValidateAsync(row.TenantId, row.Track, next, cancellationToken);

        return new MigrationApplyPreviewDto(
            DryRun: dryRun,
            CurrentVersion: currentLabel,
            Track: row.Track,
            WouldApply: new MigrationPlanItemDto(next.Version, next.Description, next.Tags));
    }

    private async Task<Application.Migrations.MigrationInfo> ResolveNextMigrationAsync(
        TenantSchemaVersion row,
        long? targetVersion,
        CancellationToken cancellationToken)
    {
        var preview = await BuildPreviewAsync(row, targetVersion, expectedUpdatedAt: null, dryRun: false, cancellationToken);
        if (preview.WouldApply is null)
        {
            throw new ConflictException(
                $"Tenant '{row.TenantId}' has no pending migrations on track '{row.Track}'.");
        }

        return plans.Find(preview.WouldApply.Version)
            ?? throw new InvalidOperationException(
                $"Migration version {preview.WouldApply.Version} is not in catalog.");
    }

    private static void AssertExpectedUpdatedAt(TenantSchemaVersion row, DateTimeOffset? expectedUpdatedAt)
    {
        if (expectedUpdatedAt is null)
            return;

        if (row.UpdatedAt != expectedUpdatedAt.Value)
        {
            throw new ConflictException(
                "Tenant schema version was modified by another operation. "
                + "Reload the migration plan and retry with the current UpdatedAt.");
        }
    }
}
