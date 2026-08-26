using FluentMigrator.Runner;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TodoPlatform.Application.Tenancy;

namespace TodoPlatform.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>
    /// Applies FluentMigrator + <see cref="DbSeeder"/> when <c>Database:AutoMigrate</c> is true (compose / local).
    /// </summary>
    public static async Task MigrateOnStartupAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue("Database:AutoMigrate", false))
            return;

        using var scope = app.Services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseInitializer");

        try
        {
            runner.MigrateUp();
            logger.LogInformation("FluentMigrator: database migrations applied.");

            var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
            tenantContext.Set(
                Domain.Tenancy.WellKnownTenants.DefaultId,
                Domain.Tenancy.WellKnownTenants.DefaultSlug,
                Domain.Tenancy.TenantSchemaNaming.FromSlug(Domain.Tenancy.WellKnownTenants.DefaultSlug));

            var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
            await seeder.SeedAsync();
            logger.LogInformation("Database seed applied (test user and sample todos).");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "FluentMigrator: migrations/seed skipped (is Postgres running?).");
        }
    }

    /// <summary>Obsolete name — use <see cref="MigrateOnStartupAsync"/>.</summary>
    public static Task MigrateDevDatabaseAsync(this WebApplication app) =>
        MigrateOnStartupAsync(app);
}
