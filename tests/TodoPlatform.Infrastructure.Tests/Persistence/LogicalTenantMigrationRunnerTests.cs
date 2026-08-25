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
        var runner = CreateRunner(db);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            runner.ApplyAsync(WellKnownTenants.DefaultId, null, "admin@test", cancellationToken: CancellationToken.None));

        Assert.Contains("no pending", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApplyAsync_BetaTenantWithoutTodos_BumpsToV012AndWritesHistory()
    {
        await using var db = await SeedAsync(MigrationTracks.Beta, currentVersion: 11, seedTodos: false);
        var runner = CreateRunner(db);

        var result = await runner.ApplyAsync(
            WellKnownTenants.DefaultId,
            targetVersion: 12,
            appliedBy: "admin@test",
            cancellationToken: CancellationToken.None);
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
    public async Task ApplyAsync_BetaWithTodos_ThrowsIncompatibleConflict()
    {
        await using var db = await SeedAsync(MigrationTracks.Beta, currentVersion: 11, seedTodos: true);
        var runner = CreateRunner(db);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            runner.ApplyAsync(WellKnownTenants.DefaultId, null, "admin", cancellationToken: CancellationToken.None));

        Assert.Contains("incompatible", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PreviewAsync_DryRun_ReturnsWouldApplyWithoutPersisting()
    {
        await using var db = await SeedAsync(MigrationTracks.Beta, currentVersion: 11, seedTodos: false);
        var runner = CreateRunner(db);

        var preview = await runner.PreviewAsync(WellKnownTenants.DefaultId, null, cancellationToken: CancellationToken.None);

        Assert.True(preview.DryRun);
        Assert.NotNull(preview.WouldApply);
        Assert.Equal(12, preview.WouldApply!.Version);
        Assert.Equal(11, (await db.TenantSchemaVersions.SingleAsync()).CurrentVersion);
    }

    [Fact]
    public async Task ApplyAsync_StaleExpectedUpdatedAt_ThrowsConflict()
    {
        await using var db = await SeedAsync(MigrationTracks.Beta, currentVersion: 11, seedTodos: false);
        var runner = CreateRunner(db);
        var stale = DateTimeOffset.Parse("2019-01-01T00:00:00Z");

        await Assert.ThrowsAsync<ConflictException>(() =>
            runner.ApplyAsync(
                WellKnownTenants.DefaultId,
                null,
                "admin",
                expectedUpdatedAt: stale,
                CancellationToken.None));
    }

    private static LogicalTenantMigrationRunner CreateRunner(AppDbContext db) =>
        new(
            db,
            new EfTenantSchemaVersionStore(db),
            new MigrationPlanService(),
            new TenantMigrationCompatibilityValidator(db));

    private static async Task<AppDbContext> SeedAsync(
        string track,
        long currentVersion,
        bool seedTodos = false)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        var tenantId = WellKnownTenants.DefaultId;
        db.Tenants.Add(
            Tenant.Create(WellKnownTenants.DefaultSlug, WellKnownTenants.DefaultName, tenantId));
        db.TenantSchemaVersions.Add(
            TenantSchemaVersion.Create(tenantId, track, currentVersion));

        if (seedTodos)
        {
            var user = User.Register("u@test.com", "hash", "U", tenantId);
            db.Users.Add(user);
            db.Todos.Add(Todo.Create("seed", user.Id, tenantId: tenantId));
        }

        await db.SaveChangesAsync();
        return db;
    }
}
