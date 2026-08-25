using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoPlatform.Domain.Entities;
using TodoPlatform.Domain.Tenancy;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Api.Tests;

internal static class AdminTestData
{
    public static async Task ResetTenantSchemaAsync(
        IServiceProvider services,
        Guid tenantId,
        string track,
        long version = 11)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.TenantSchemaVersions.SingleOrDefaultAsync(v => v.TenantId == tenantId);
        if (row is not null)
            db.TenantSchemaVersions.Remove(row);

        var history = await db.MigrationHistory.Where(h => h.TenantId == tenantId).ToListAsync();
        if (history.Count > 0)
            db.MigrationHistory.RemoveRange(history);

        db.TenantSchemaVersions.Add(TenantSchemaVersion.Create(tenantId, track, version));
        await db.SaveChangesAsync();
    }
}
