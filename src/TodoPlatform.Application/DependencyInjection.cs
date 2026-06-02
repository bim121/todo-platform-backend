using Microsoft.Extensions.DependencyInjection;
using TodoPlatform.Application.Services;

namespace TodoPlatform.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ITodoService, TodoService>();
        return services;
    }
}
