using TodoPlatform.Application.Caching;
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
using StackExchange.Redis;

namespace TodoPlatform.Infrastructure;

public static class DependencyInjection
{
    public const string RedisInstanceName = "TodoPlatform:";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (configuration.GetValue("Database:UseInMemory", false))
        {
            var databaseName = configuration.GetValue<string>("Database:InMemoryName")
                ?? "TodoPlatformTests";

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));
        }
        else
        {
            var connectionString = configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddFluentMigrator(connectionString);
        }

        services.AddScoped<DbSeeder>();
        services.AddRepositories();
        services.AddScoped<IUserSyncService, UserSyncService>();
        services.AddSingleton<IPasswordHasher, Sha256PasswordHasher>();
        services.AddCaching(configuration);

        return services;
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
