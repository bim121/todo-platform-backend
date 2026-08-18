using Microsoft.EntityFrameworkCore;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Tenancy;
using TodoPlatform.Domain.Entities;
using TodoPlatform.Domain.Enums;
using TodoPlatform.Domain.Tenancy;
using TodoPlatform.Infrastructure.Tenancy;

namespace TodoPlatform.Infrastructure.Persistence;

public sealed class DbSeeder(
    AppDbContext db,
    IPasswordHasher passwordHasher,
    ITenantContext tenantContext,
    IMigrationPlanService migrationPlans)
{
    public const string TestEmail = "test@example.com";
    public const string TestPassword = "password123";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await EnsureTenantsAsync(cancellationToken);
        await EnsureSchemaVersionsAsync(cancellationToken);

        tenantContext.Set(WellKnownTenants.DefaultId, WellKnownTenants.DefaultSlug);
        await ApplyTenantToOpenConnectionAsync(cancellationToken);

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == TestEmail, cancellationToken);

        if (user is null)
        {
            user = User.Register(
                TestEmail,
                passwordHasher.Hash(TestPassword),
                "Test User",
                WellKnownTenants.DefaultId);
            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);
        }
        else if (user.TenantId == Guid.Empty)
        {
            user.AssignTenant(WellKnownTenants.DefaultId);
            await db.SaveChangesAsync(cancellationToken);
        }

        if (await db.Todos.AnyAsync(t => t.UserId == user.Id, cancellationToken))
            return;

        db.Todos.AddRange(
            Todo.Create("Learn NgRx Effects", user.Id, tenantId: WellKnownTenants.DefaultId),
            Todo.Create(
                "Connect Angular to ASP.NET API",
                user.Id,
                TodoStatus.InProgress,
                TodoPriority.High,
                WellKnownTenants.DefaultId),
            Todo.Create(
                "Review OpenAPI contract",
                user.Id,
                priority: TodoPriority.Low,
                tenantId: WellKnownTenants.DefaultId));

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureTenantsAsync(CancellationToken cancellationToken)
    {
        if (await db.Tenants.AnyAsync(cancellationToken))
            return;

        db.Tenants.AddRange(
            Tenant.Create(WellKnownTenants.DefaultSlug, WellKnownTenants.DefaultName, WellKnownTenants.DefaultId),
            Tenant.Create(WellKnownTenants.AcmeSlug, WellKnownTenants.AcmeName, WellKnownTenants.AcmeId));
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureSchemaVersionsAsync(CancellationToken cancellationToken)
    {
        var existing = await db.TenantSchemaVersions
            .Select(v => v.TenantId)
            .ToListAsync(cancellationToken);
        var tenantIds = await db.Tenants.Select(t => t.Id).ToListAsync(cancellationToken);
        var latest = migrationPlans.LatestStableVersion;

        foreach (var tenantId in tenantIds.Except(existing))
        {
            db.TenantSchemaVersions.Add(
                TenantSchemaVersion.Create(tenantId, MigrationTracks.Stable, latest));
        }

        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyTenantToOpenConnectionAsync(CancellationToken cancellationToken)
    {
        if (!db.Database.IsRelational())
            return;

        var connection = db.Database.GetDbConnection();
        if (connection.State == System.Data.ConnectionState.Open)
            await TenantSession.ApplyAsync(connection, tenantContext.TenantId, cancellationToken);
    }
}
