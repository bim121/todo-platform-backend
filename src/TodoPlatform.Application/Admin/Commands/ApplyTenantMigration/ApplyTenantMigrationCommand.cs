using MediatR;
using TodoPlatform.Application.Common;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Exceptions;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Services;

namespace TodoPlatform.Application.Admin.Commands.ApplyTenantMigration;

/// <summary>
/// Apply next (or explicit next) pending migration for a tenant (B-12.5 / B-12.7).
/// </summary>
public sealed record ApplyTenantMigrationCommand(
    Guid TenantId,
    long? TargetVersion = null,
    DateTimeOffset? ExpectedUpdatedAt = null,
    bool DryRun = false) : IRequest<ApplyTenantMigrationResponse>, ICommand;

public sealed class ApplyTenantMigrationHandler(
    ITenantAdminReadStore tenants,
    ITenantMigrationRunner runner,
    ICurrentUserService currentUser)
    : IRequestHandler<ApplyTenantMigrationCommand, ApplyTenantMigrationResponse>
{
    public async Task<ApplyTenantMigrationResponse> Handle(
        ApplyTenantMigrationCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await tenants.GetByIdAsync(request.TenantId, cancellationToken);
        if (existing is null)
            throw new NotFoundException($"Tenant '{request.TenantId}' was not found.");

        if (request.DryRun)
        {
            var preview = await runner.PreviewAsync(
                request.TenantId,
                request.TargetVersion,
                request.ExpectedUpdatedAt,
                cancellationToken);

            return new ApplyTenantMigrationResponse(DryRun: true, Tenant: null, Preview: preview);
        }

        var appliedBy = currentUser.Email
            ?? currentUser.KeycloakSub
            ?? currentUser.Name
            ?? "admin";

        var result = await runner.ApplyAsync(
            request.TenantId,
            request.TargetVersion,
            appliedBy,
            request.ExpectedUpdatedAt,
            cancellationToken);

        var tenant = existing with
        {
            SchemaVersion = result.SchemaVersionLabel,
            DeploymentTrack = result.Track
        };

        return new ApplyTenantMigrationResponse(DryRun: false, Tenant: tenant, Preview: null);
    }
}
