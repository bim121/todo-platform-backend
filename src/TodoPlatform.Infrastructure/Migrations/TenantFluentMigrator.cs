using FluentMigrator.Runner;
using FluentMigrator.Runner.Initialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Infrastructure.Migrations;

public sealed class TenantFluentMigrator(IConfiguration configuration) : ITenantFluentMigrator
{
    public void MigrateUp(string schemaName, long targetVersion)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

        var tenantConnectionString = TenantConnectionStrings.WithSearchPath(connectionString, schemaName);

        var services = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(tenantConnectionString)
                .ScanIn(typeof(T1001_TenantSchemaBaseline).Assembly).For.Migrations())
            .Configure<RunnerOptions>(options =>
            {
                options.Tags = ["tenant"];
                options.IncludeUntaggedMigrations = false;
            })
            .AddLogging(lb => lb.AddFluentMigratorConsole())
            .BuildServiceProvider();

        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMigrationRunner>().MigrateUp(targetVersion);
    }
}
