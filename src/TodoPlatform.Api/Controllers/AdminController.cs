using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoPlatform.Application.Admin.Queries.GetTenants;
using TodoPlatform.Application.Dtos;

namespace TodoPlatform.Api.Controllers;

/// <summary>
/// Admin stubs (B-05.6). Full implementation in B-12+.
/// </summary>
[ApiController]
[Route("api/admin/tenants")]
[Authorize(Roles = "admin")]
[Produces("application/json")]
public sealed class AdminController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// List all tenants (admin only).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TenantAdminDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<TenantAdminDto>>> GetTenants(
        CancellationToken cancellationToken)
    {
        var tenants = await mediator.Send(new GetTenantsQuery(), cancellationToken);
        return Ok(tenants);
    }
}
