using FluentMigrator.Runner;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TodoPlatform.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task MigrateDevDatabaseAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return;

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

            var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
            await seeder.SeedAsync();
            logger.LogInformation("Database seed applied (test user and sample todos).");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "FluentMigrator: migrations/seed skipped (is Postgres running?).");
        }

    }
}
