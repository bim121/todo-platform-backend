using Microsoft.Extensions.DependencyInjection;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Infrastructure.Messaging;
using TodoPlatform.Infrastructure.Persistence;
using TodoPlatform.Infrastructure.Repositories;

namespace TodoPlatform.Infrastructure.Repositories;

public static class RepositoryRegistration
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<ITodoRepository, TodoRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddSingleton<IDomainEventToIntegrationEventMapper, DomainEventToIntegrationEventMapper>();
        services.AddScoped<IOutboxStore, EfOutboxStore>();
        services.AddScoped<IProcessedMessageStore, EfProcessedMessageStore>();
        services.AddScoped<ISpecificationEvaluator, SpecificationEvaluator>();
        return services;
    }
}
