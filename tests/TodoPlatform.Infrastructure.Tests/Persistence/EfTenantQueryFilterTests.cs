using Microsoft.EntityFrameworkCore;
using TodoPlatform.Application.Tenancy;
using TodoPlatform.Domain.Entities;
using TodoPlatform.Domain.Tenancy;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Infrastructure.Tests.Persistence;

public sealed class EfTenantQueryFilterTests
{
    [Fact]
    public async Task Todos_AreScopedToResolvedTenant()
    {
        var tenantContext = new TenantContext();
        await using var db = CreateDb(tenantContext);

        db.Tenants.AddRange(
            Tenant.Create(WellKnownTenants.DefaultSlug, WellKnownTenants.DefaultName, WellKnownTenants.DefaultId),
            Tenant.Create(WellKnownTenants.AcmeSlug, WellKnownTenants.AcmeName, WellKnownTenants.AcmeId));
        var defaultUser = User.Register("default@example.com", "hash", "Default", WellKnownTenants.DefaultId);
        var acmeUser = User.Register("acme@example.com", "hash", "Acme", WellKnownTenants.AcmeId);
        db.Users.AddRange(defaultUser, acmeUser);
        db.Todos.AddRange(
            Todo.Create("default-secret", defaultUser.Id, tenantId: WellKnownTenants.DefaultId),
            Todo.Create("acme-secret", acmeUser.Id, tenantId: WellKnownTenants.AcmeId));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        tenantContext.Set(WellKnownTenants.DefaultId, WellKnownTenants.DefaultSlug);
        var defaultTitles = await db.Todos.Select(t => t.Title).ToListAsync();
        Assert.Equal(["default-secret"], defaultTitles);

        tenantContext.Set(WellKnownTenants.AcmeId, WellKnownTenants.AcmeSlug);
        var acmeTitles = await db.Todos.Select(t => t.Title).ToListAsync();
        Assert.Equal(["acme-secret"], acmeTitles);
    }

    [Fact]
    public async Task SystemStats_IgnoreQueryFilters_CountAllTenants()
    {
        var tenantContext = new TenantContext();
        tenantContext.Set(WellKnownTenants.DefaultId, WellKnownTenants.DefaultSlug);
        await using var db = CreateDb(tenantContext);

        db.Users.AddRange(
            User.Register("a@example.com", "hash", "A", WellKnownTenants.DefaultId),
            User.Register("b@example.com", "hash", "B", WellKnownTenants.AcmeId));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        Assert.Equal(1, await db.Users.CountAsync());

        var stats = await new EfSystemStatsReadStore(db).GetAsync();
        Assert.Equal(2, stats.TotalUsers);
    }

    private static AppDbContext CreateDb(ITenantContext tenantContext)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options, tenantContext);
    }
}
