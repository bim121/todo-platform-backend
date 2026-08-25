using MassTransit;
using TodoPlatform.Api.Configuration;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Infrastructure.Messaging;
using TodoPlatform.Infrastructure.Messaging.Consumers;

namespace TodoPlatform.Api.Extensions;

public static class MessagingExtensions
{
    public const string TodoCreatedEmailEndpoint = "todo-created-email";
    public const string TodoCompletedNotificationEndpoint = "todo-completed-notification";
    public const string TenantMigrationAppliedNotificationEndpoint = "tenant-migration-applied-notification";

    public static IServiceCollection AddApiMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.AddSingleton<IEmailSender, EmailSender>();

        var options = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>()
            ?? new RabbitMqOptions();

        if (!options.Enabled || environment.IsEnvironment("Testing"))
            return services;

        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<SendTodoCreatedEmailConsumer>();
            bus.AddConsumer<TodoCompletedNotificationConsumer>();
            bus.AddConsumer<TenantMigrationAppliedNotificationConsumer>();

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(options.Host, options.Port, options.VirtualHost, h =>
                {
                    h.Username(options.Username);
                    h.Password(options.Password);
                });

                cfg.ReceiveEndpoint(TodoCreatedEmailEndpoint, e =>
                {
                    ConfigureRetry(e);
                    e.ConfigureConsumer<SendTodoCreatedEmailConsumer>(context);
                });

                cfg.ReceiveEndpoint(TodoCompletedNotificationEndpoint, e =>
                {
                    ConfigureRetry(e);
                    e.ConfigureConsumer<TodoCompletedNotificationConsumer>(context);
                });

                cfg.ReceiveEndpoint(TenantMigrationAppliedNotificationEndpoint, e =>
                {
                    ConfigureRetry(e);
                    e.ConfigureConsumer<TenantMigrationAppliedNotificationConsumer>(context);
                });
            });
        });

        services.AddHostedService<OutboxProcessor>();

        return services;
    }

    private static void ConfigureRetry(IReceiveEndpointConfigurator endpoint)
    {
        // 3 attempts with exponential backoff (B-07.7). Failed messages go to _error queue.
        endpoint.UseMessageRetry(r => r.Exponential(
            retryLimit: 3,
            minInterval: TimeSpan.FromSeconds(1),
            maxInterval: TimeSpan.FromSeconds(30),
            intervalDelta: TimeSpan.FromSeconds(2)));
    }
}
