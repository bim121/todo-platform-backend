using TodoPlatform.Application.Caching;
using TodoPlatform.Application.Diagnostics;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Services;
using TodoPlatform.Infrastructure.Caching;
using TodoPlatform.Infrastructure.Migrations;
using TodoPlatform.Infrastructure.Persistence;
using TodoPlatform.Infrastructure.Repositories;
using TodoPlatform.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace TodoPlatform.Infrastructure;

public static class DependencyInjection
{
    public const string RedisInstanceName = "TodoPlatform:";

    /// <summary>Appended when the connection string has no pool size (B-09.6).</summary>
    public const string DefaultNpgsqlPoolSettings = "Maximum Pool Size=100;Minimum Pool Size=0;Timeout=15";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<SlowQueryMetrics>();
        services.AddSingleton<SlowQueryInterceptor>();

        if (configuration.GetValue("Database:UseInMemory", false))
        {
            var databaseName = configuration.GetValue<string>("Database:InMemoryName")
                ?? "TodoPlatformTests";

            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase(databaseName);
                options.AddInterceptors(sp.GetRequiredService<SlowQueryInterceptor>());
            });

            // No SQL view / Npgsql in tests — EF aggregate mimics the read model.
            services.AddScoped<ITodoStatsReadStore, EfTodoStatsReadStore>();
            services.AddScoped<ITodoFilterReadStore, EfTodoFilterReadStore>();
        }
        else
        {
            var connectionString = EnsurePoolSettings(
                configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException("Connection string 'Default' is not configured."));

            var readConnectionString = EnsurePoolSettings(
                configuration.GetConnectionString("Read") ?? connectionString);

            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.UseNpgsql(connectionString);
                options.AddInterceptors(sp.GetRequiredService<SlowQueryInterceptor>());

                var env = sp.GetService<IHostEnvironment>();
                if (env?.IsDevelopment() == true)
                {
                    // B-09.5 — see parameter values in EF SQL logs (dev only).
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }
            });

            services.AddFluentMigrator(connectionString);

            // B-10.1 — separate read connection (same DB until a replica is introduced).
            services.AddSingleton<IReadDbConnection>(_ => new DapperReadDbConnection(readConnectionString));
            services.AddScoped<ITodoStatsReadStore, DapperTodoStatsReadStore>();
            services.AddScoped<ITodoFilterReadStore, DapperTodoFilterReadStore>();
        }

        services.AddScoped<DbSeeder>();
        services.AddRepositories();
        services.AddScoped<IUserSyncService, UserSyncService>();
        services.AddSingleton<IPasswordHasher, Sha256PasswordHasher>();
        services.AddSingleton<CacheMetrics>();
        services.AddCaching(configuration);

        return services;
    }

    public static string EnsurePoolSettings(string connectionString)
    {
        if (connectionString.Contains("Maximum Pool Size", StringComparison.OrdinalIgnoreCase))
            return connectionString;

        var trimmed = connectionString.TrimEnd().TrimEnd(';');
        return $"{trimmed};{DefaultNpgsqlPoolSettings}";
    }

    private static IServiceCollection AddCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var useMemory = configuration.GetValue("Cache:UseMemory", false)
            || configuration.GetValue("Database:UseInMemory", false);

        if (useMemory)
        {
            services.AddDistributedMemoryCache();
            services.AddSingleton<ICacheService, MemoryCacheService>();
            return services;
        }

        var redisConnection = configuration.GetConnectionString("Redis")
            ?? "localhost:6379";

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = RedisInstanceName;
        });

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnection));

        services.AddSingleton<ICacheService, RedisCacheService>();
        return services;
    }
}
