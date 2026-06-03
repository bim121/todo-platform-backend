using FluentMigrator.Runner;
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

        return services;
    }
}
