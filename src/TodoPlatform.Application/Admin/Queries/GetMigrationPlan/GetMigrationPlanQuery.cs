using MediatR;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Exceptions;
using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Application.Admin.Queries.GetMigrationPlan;

/// <summary>Pending migrations for a tenant's track (B-12.6).</summary>
public sealed record GetMigrationPlanQuery(Guid TenantId) : IRequest<MigrationPlanDto>;

public sealed class GetMigrationPlanQueryHandler(
    ITenantAdminReadStore tenants,
    ITenantSchemaVersionStore versions,
    IMigrationPlanService plans)
    : IRequestHandler<GetMigrationPlanQuery, MigrationPlanDto>
{
    public async Task<MigrationPlanDto> Handle(
        GetMigrationPlanQuery request,
        CancellationToken cancellationToken)
    {
        var tenant = await tenants.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null)
            throw new NotFoundException($"Tenant '{request.TenantId}' was not found.");

        var state = await versions.GetAsync(request.TenantId, cancellationToken);
        var track = state?.Track ?? tenant.DeploymentTrack;
        var current = state?.CurrentVersion ?? 0;
        var updatedAt = state?.UpdatedAt ?? DateTimeOffset.MinValue;
        var label = plans.Find(current)?.SchemaVersionLabel ?? $"V{current:D3}";

        var pending = plans.GetPending(track, current)
            .Select(m => new MigrationPlanItemDto(m.Version, m.Description, m.Tags))
            .ToArray();

        return new MigrationPlanDto(label, track, updatedAt, pending);
    }
}
