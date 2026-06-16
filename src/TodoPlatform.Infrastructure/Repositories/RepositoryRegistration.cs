using Microsoft.Extensions.DependencyInjection;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Infrastructure.Repositories;

public static class RepositoryRegistration
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<ITodoRepository, TodoRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<ISpecificationEvaluator, SpecificationEvaluator>();
        return services;
    }
}
