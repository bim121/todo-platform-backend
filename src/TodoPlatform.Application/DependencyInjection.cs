using Microsoft.Extensions.DependencyInjection;

namespace TodoPlatform.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // MediatR, FluentValidation — Phase B-03
        return services;
    }
}
