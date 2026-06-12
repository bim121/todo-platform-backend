using MediatR;
using Microsoft.EntityFrameworkCore;
using TodoPlatform.Application.Common;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Infrastructure.Behaviors;

public sealed class TransactionBehavior<TRequest, TResponse>(AppDbContext dbContext)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICommand || !dbContext.Database.IsRelational())
            return await next();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var response = await next();
            await transaction.CommitAsync(cancellationToken);
            return response;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
