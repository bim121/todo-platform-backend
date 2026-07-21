using MassTransit;
using TodoPlatform.Api.Configuration;
using TodoPlatform.Infrastructure;

namespace TodoPlatform.Api.Extensions;

public static class MessagingExtensions
{
    public const string TodoCreatedEmailEndpoint = "todo-created-email";

    public static IServiceCollection AddApiMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));

        var options = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>()
            ?? new RabbitMqOptions();

        if (!options.Enabled || environment.IsEnvironment("Testing"))
            return services;

        services.AddMassTransit(bus =>
        {
            bus.AddConsumers(typeof(DependencyInjection).Assembly);

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(options.Host, options.Port, options.VirtualHost, h =>
                {
                    h.Username(options.Username);
                    h.Password(options.Password);
                });

                // Queue for TodoCreatedIntegrationEvent email notifications (consumer in B-07.7).
                cfg.ReceiveEndpoint(TodoCreatedEmailEndpoint, e =>
                {
                    e.ConfigureConsumers(context);
                });
            });
        });

        return services;
    }
}
