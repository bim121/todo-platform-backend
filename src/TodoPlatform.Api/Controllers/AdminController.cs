using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoPlatform.Application.Admin.Commands.ApplyTenantMigration;
using TodoPlatform.Application.Admin.Queries.GetMigrationPlan;
using TodoPlatform.Application.Admin.Queries.GetSystemStats;
using TodoPlatform.Application.Admin.Queries.GetTenantById;
using TodoPlatform.Application.Admin.Queries.GetTenants;
using TodoPlatform.Application.Dtos;

namespace TodoPlatform.Api.Controllers;

/// <summary>
/// Admin endpoints. Tenants + schema versions (B-12); system stats read model (B-10.7).
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "admin")]
[Produces("application/json")]
public sealed class AdminController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// List tenants with logical schema version and track (admin only). Supports skip/take and track/status filters.
    /// </summary>
    [HttpGet("tenants")]
    [ProducesResponseType(typeof(PagedResult<TenantAdminDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<TenantAdminDto>>> GetTenants(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] string? track = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var page = await mediator.Send(
            new GetTenantsQuery(skip, take, track, status),
            cancellationToken);
        return Ok(page);
    }

    /// <summary>
    /// Tenant detail including schema version (admin only).
    /// </summary>
    [HttpGet("tenants/{id:guid}")]
    [ProducesResponseType(typeof(TenantAdminDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TenantAdminDto>> GetTenantById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var tenant = await mediator.Send(new GetTenantByIdQuery(id), cancellationToken);
        return Ok(tenant);
    }

    /// <summary>
    /// Pending migrations for the tenant's track (B-12.6).
    /// </summary>
    [HttpGet("tenants/{id:guid}/migration-plan")]
    [ProducesResponseType(typeof(MigrationPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MigrationPlanDto>> GetMigrationPlan(
        Guid id,
        CancellationToken cancellationToken)
    {
        var plan = await mediator.Send(new GetMigrationPlanQuery(id), cancellationToken);
        return Ok(plan);
    }

    /// <summary>
    /// Apply the next pending migration (or the given next target) for a tenant (B-12.5).
    /// Week 2: logical version + history only.
    /// </summary>
    [HttpPost("tenants/{id:guid}/migrations/apply")]
    [ProducesResponseType(typeof(TenantAdminDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TenantAdminDto>> ApplyMigration(
        Guid id,
        [FromBody] ApplyTenantMigrationRequest? body,
        CancellationToken cancellationToken)
    {
        var tenant = await mediator.Send(
            new ApplyTenantMigrationCommand(id, body?.TargetVersion),
            cancellationToken);
        return Ok(tenant);
    }

    /// <summary>
    /// Platform-wide aggregates (users, todos, avg todos/user). Dapper + JOIN.
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(SystemStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SystemStatsDto>> GetSystemStats(
        CancellationToken cancellationToken)
    {
        var stats = await mediator.Send(new GetSystemStatsQuery(), cancellationToken);
        return Ok(stats);
    }
}
