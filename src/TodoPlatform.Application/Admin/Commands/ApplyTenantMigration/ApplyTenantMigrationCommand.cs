using MediatR;
using TodoPlatform.Application.Common;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Exceptions;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Services;

namespace TodoPlatform.Application.Admin.Commands.ApplyTenantMigration;

/// <summary>
/// Apply next (or explicit next) pending migration for a tenant (B-12.5).
/// Week 2: logical version + history; DDL in tenant schema is B-12.12.
/// </summary>
public sealed record ApplyTenantMigrationCommand(
    Guid TenantId,
    long? TargetVersion = null) : IRequest<TenantAdminDto>, ICommand;

public sealed class ApplyTenantMigrationHandler(
    ITenantAdminReadStore tenants,
    ITenantMigrationRunner runner,
    ICurrentUserService currentUser)
    : IRequestHandler<ApplyTenantMigrationCommand, TenantAdminDto>
{
    public async Task<TenantAdminDto> Handle(
        ApplyTenantMigrationCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await tenants.GetByIdAsync(request.TenantId, cancellationToken);
        if (existing is null)
            throw new NotFoundException($"Tenant '{request.TenantId}' was not found.");

        var appliedBy = currentUser.Email
            ?? currentUser.KeycloakSub
            ?? currentUser.Name
            ?? "admin";

        var result = await runner.ApplyAsync(
            request.TenantId,
            request.TargetVersion,
            appliedBy,
            cancellationToken);

        return existing with
        {
            SchemaVersion = result.SchemaVersionLabel,
            DeploymentTrack = result.Track
        };
    }
}
