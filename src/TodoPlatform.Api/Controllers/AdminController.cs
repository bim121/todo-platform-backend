using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    /// List all tenants with logical schema version and track (admin only).
    /// </summary>
    [HttpGet("tenants")]
    [ProducesResponseType(typeof(IReadOnlyList<TenantAdminDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<TenantAdminDto>>> GetTenants(
        CancellationToken cancellationToken)
    {
        var tenants = await mediator.Send(new GetTenantsQuery(), cancellationToken);
        return Ok(tenants);
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
