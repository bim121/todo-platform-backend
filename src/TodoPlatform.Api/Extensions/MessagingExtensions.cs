using MassTransit;
using TodoPlatform.Api.Configuration;
using TodoPlatform.Infrastructure;
using TodoPlatform.Infrastructure.Messaging;
using TodoPlatform.Infrastructure.Messaging.Consumers;

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
            bus.AddConsumer<SendTodoCreatedEmailConsumer>();

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(options.Host, options.Port, options.VirtualHost, h =>
                {
                    h.Username(options.Username);
                    h.Password(options.Password);
                });

                cfg.ReceiveEndpoint(TodoCreatedEmailEndpoint, e =>
                {
                    e.ConfigureConsumer<SendTodoCreatedEmailConsumer>(context);
                });
            });
        });

        services.AddHostedService<OutboxProcessor>();

        return services;
    }
}
