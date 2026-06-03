using FluentMigrator.Runner;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TodoPlatform.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static Task MigrateDevDatabaseAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return Task.CompletedTask;

        if (!app.Configuration.GetValue("Database:AutoMigrate", false))
            return Task.CompletedTask;

        using var scope = app.Services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseInitializer");

        try
        {
            runner.MigrateUp();
            logger.LogInformation("FluentMigrator: database migrations applied.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "FluentMigrator: migrations skipped (is Postgres running?).");
        }

        return Task.CompletedTask;
    }
}
