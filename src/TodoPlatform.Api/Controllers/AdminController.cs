using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoPlatform.Application.Admin.Queries.GetSystemStats;
using TodoPlatform.Application.Admin.Queries.GetTenants;
using TodoPlatform.Application.Dtos;

namespace TodoPlatform.Api.Controllers;

/// <summary>
/// Admin endpoints. Tenants stubs (B-05.6 / B-12+); system stats read model (B-10.7).
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "admin")]
[Produces("application/json")]
public sealed class AdminController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// List all tenants (admin only). Stub until B-12.
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
