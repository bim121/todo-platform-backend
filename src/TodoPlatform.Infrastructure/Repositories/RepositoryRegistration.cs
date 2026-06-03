using Microsoft.Extensions.DependencyInjection;
using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Infrastructure.Repositories;

public static class RepositoryRegistration
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<ITodoRepository, TodoRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        return services;
    }
}
