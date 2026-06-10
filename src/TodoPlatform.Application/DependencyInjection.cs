using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TodoPlatform.Application.Services;

namespace TodoPlatform.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly));

        services.AddValidatorsFromAssembly(typeof(ApplicationAssemblyMarker).Assembly);

        services.AddScoped<ITodoService, TodoService>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
