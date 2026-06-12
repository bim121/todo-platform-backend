using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TodoPlatform.Application.Behaviors;
using TodoPlatform.Application.Services;

namespace TodoPlatform.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        Action<MediatRServiceConfiguration>? configureMediatR = null)
    {
        services.AddMediatR(cfg =>
        {
            configureMediatR?.Invoke(cfg);
            cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(ApplicationAssemblyMarker).Assembly);

        services.AddScoped<ITodoService, TodoService>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
