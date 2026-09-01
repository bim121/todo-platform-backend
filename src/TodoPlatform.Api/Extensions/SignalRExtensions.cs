using StackExchange.Redis;
using TodoPlatform.Api.Hubs;

namespace TodoPlatform.Api.Extensions;

public static class SignalRExtensions
{
    public const string RedisChannelPrefix = "TodoPlatform:SignalR:";

    public static IServiceCollection AddApiSignalR(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var signalR = services.AddSignalR(options =>
        {
            if (environment.IsDevelopment())
                options.EnableDetailedErrors = true;
        });

        if (ShouldUseRedisBackplane(configuration, environment))
        {
            var redis = configuration.GetConnectionString("Redis") ?? "localhost:6379";
            signalR.AddStackExchangeRedis(redis, options =>
            {
                options.Configuration.ChannelPrefix = RedisChannel.Literal(RedisChannelPrefix);
            });
        }

        return services;
    }

    public static WebApplication MapApiHubs(this WebApplication app)
    {
        app.MapHub<TodoHub>("/hubs/todos");
        return app;
    }

    private static bool ShouldUseRedisBackplane(IConfiguration configuration, IHostEnvironment environment)
    {
        if (environment.IsEnvironment("Testing"))
            return false;

        if (configuration.GetValue("Database:UseInMemory", false))
            return false;

        if (configuration.GetValue("Cache:UseMemory", false))
            return false;

        return configuration.GetValue("SignalR:UseRedisBackplane", true);
    }
}
