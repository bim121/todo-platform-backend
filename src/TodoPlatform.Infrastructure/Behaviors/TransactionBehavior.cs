using MediatR;
using Microsoft.EntityFrameworkCore;
using TodoPlatform.Application.Admin.Commands.ApplyTenantMigration;
using TodoPlatform.Application.Common;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Infrastructure.Behaviors;

public sealed class TransactionBehavior<TRequest, TResponse>(
    AppDbContext dbContext,
    IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is ApplyTenantMigrationCommand { DryRun: true })
            return await next();

        if (request is not ICommand)
            return await next();

        if (!dbContext.Database.IsRelational())
            return await ExecuteWithoutTransactionAsync(next, cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var response = await next();
            await unitOfWork.CommitAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return response;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<TResponse> ExecuteWithoutTransactionAsync(
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await next();
            await unitOfWork.CommitAsync(cancellationToken);
            return response;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
