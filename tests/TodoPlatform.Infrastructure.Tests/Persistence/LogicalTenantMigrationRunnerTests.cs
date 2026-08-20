using Microsoft.EntityFrameworkCore;
using TodoPlatform.Application.Exceptions;
using TodoPlatform.Domain.Entities;
using TodoPlatform.Domain.Events;
using TodoPlatform.Domain.Tenancy;
using TodoPlatform.Infrastructure.Migrations;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Infrastructure.Tests.Persistence;

public sealed class LogicalTenantMigrationRunnerTests
{
    [Fact]
    public async Task ApplyAsync_StableTenant_ConflictsWhenNoPending()
    {
        await using var db = await SeedAsync(MigrationTracks.Stable, currentVersion: 11);
        var plans = new MigrationPlanService();
        var versions = new EfTenantSchemaVersionStore(db);
        var runner = new LogicalTenantMigrationRunner(db, versions, plans);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            runner.ApplyAsync(WellKnownTenants.DefaultId, null, "admin@test", CancellationToken.None));

        Assert.Contains("no pending", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApplyAsync_BetaTenant_BumpsToV012AndWritesHistory()
    {
        await using var db = await SeedAsync(MigrationTracks.Beta, currentVersion: 11);
        var plans = new MigrationPlanService();
        var versions = new EfTenantSchemaVersionStore(db);
        var runner = new LogicalTenantMigrationRunner(db, versions, plans);

        var result = await runner.ApplyAsync(
            WellKnownTenants.DefaultId,
            targetVersion: 12,
            appliedBy: "admin@test",
            CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.Equal(12, result.AppliedVersion);
        Assert.Equal("V012-beta-feature", result.SchemaVersionLabel);

        var row = await db.TenantSchemaVersions.SingleAsync(v => v.TenantId == WellKnownTenants.DefaultId);
        Assert.Equal(12, row.CurrentVersion);

        var history = Assert.Single(db.MigrationHistory);
        Assert.Equal("V012-beta-feature", history.Version);
        Assert.Equal("admin@test", history.AppliedBy);
        Assert.Contains(history.DomainEvents, e => e is TenantMigrationAppliedEvent);
    }

    [Fact]
    public async Task ApplyAsync_WrongTarget_ThrowsConflict()
    {
        await using var db = await SeedAsync(MigrationTracks.Beta, currentVersion: 11);
        var runner = new LogicalTenantMigrationRunner(
            db,
            new EfTenantSchemaVersionStore(db),
            new MigrationPlanService());

        await Assert.ThrowsAsync<ConflictException>(() =>
            runner.ApplyAsync(WellKnownTenants.DefaultId, targetVersion: 99, "admin", CancellationToken.None));
    }

    private static async Task<AppDbContext> SeedAsync(string track, long currentVersion)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        db.Tenants.Add(
            Tenant.Create(WellKnownTenants.DefaultSlug, WellKnownTenants.DefaultName, WellKnownTenants.DefaultId));
        db.TenantSchemaVersions.Add(
            TenantSchemaVersion.Create(WellKnownTenants.DefaultId, track, currentVersion));
        await db.SaveChangesAsync();
        return db;
    }
}
