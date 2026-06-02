using Microsoft.Extensions.DependencyInjection;

namespace TodoPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // EF Core, Redis, MassTransit — later phases
        return services;
    }
}
