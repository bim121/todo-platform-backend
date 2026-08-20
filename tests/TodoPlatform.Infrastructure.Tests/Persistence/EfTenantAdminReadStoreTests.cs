using Microsoft.EntityFrameworkCore;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Domain.Entities;
using TodoPlatform.Domain.Tenancy;
using TodoPlatform.Infrastructure.Migrations;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Infrastructure.Tests.Persistence;

public sealed class EfTenantAdminReadStoreTests
{
    [Fact]
    public async Task ListAsync_ReturnsSeededTenantsOnStableTrack()
    {
        await using var db = CreateDb();
        db.Tenants.AddRange(
            Tenant.Create(WellKnownTenants.DefaultSlug, WellKnownTenants.DefaultName, WellKnownTenants.DefaultId),
            Tenant.Create(WellKnownTenants.AcmeSlug, WellKnownTenants.AcmeName, WellKnownTenants.AcmeId));
        var plans = new MigrationPlanService();
        db.TenantSchemaVersions.Add(
            TenantSchemaVersion.Create(
                WellKnownTenants.DefaultId,
                MigrationTracks.Stable,
                plans.LatestStableVersion));
        await db.SaveChangesAsync();

        var store = new EfTenantAdminReadStore(db, plans);
        var list = await store.ListAsync(new TenantAdminListFilter());

        Assert.Equal(2, list.TotalCount);
        Assert.Equal(2, list.Items.Count);
        var defaultTenant = Assert.Single(list.Items, t => t.Id == WellKnownTenants.DefaultId.ToString());
        Assert.Equal("Default", defaultTenant.Name);
        Assert.Equal("V011", defaultTenant.SchemaVersion);
        Assert.Equal(MigrationTracks.Stable, defaultTenant.DeploymentTrack);
        Assert.Equal("active", defaultTenant.Status);
    }

    [Fact]
    public async Task GetByIdAsync_Unknown_ReturnsNull()
    {
        await using var db = CreateDb();
        var store = new EfTenantAdminReadStore(db, new MigrationPlanService());
        Assert.Null(await store.GetByIdAsync(Guid.NewGuid()));
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
