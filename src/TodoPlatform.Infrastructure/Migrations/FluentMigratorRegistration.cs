using FluentMigrator.Runner;
using FluentMigrator.Runner.Initialization;
using Microsoft.Extensions.DependencyInjection;

namespace TodoPlatform.Infrastructure.Migrations;

public static class FluentMigratorRegistration
{
    public static IServiceCollection AddFluentMigrator(
        this IServiceCollection services,
        string connectionString)
    {
        services
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(V001_CreateUsersAndTodosTables).Assembly).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole());

        // B-12.2 — default MigrateUp: untagged + [Tags("stable")]. Beta-tagged migrations stay pending.
        services.Configure<RunnerOptions>(options =>
        {
            options.Tags = ["stable"];
            options.IncludeUntaggedMigrations = true;
        });

        return services;
    }
}

