using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoPlatform.Api.Versioning;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Services;

namespace TodoPlatform.Api.Controllers;

/// <summary>
/// Auth endpoints. Login is handled by Keycloak (B-05); register remains for legacy/dev until Phase 17.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(
    IAuthService authService,
    ICurrentUserService currentUser,
    IConfiguration configuration) : ControllerBase
{
    /// <summary>
    /// Deprecated mock login. Use Keycloak token endpoint instead.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [DeprecatedEndpoint("Sat, 01 Jun 2027 00:00:00 GMT")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    public ActionResult Login()
    {
        var authority = configuration["Keycloak:Authority"] ?? "http://localhost:8080/realms/todo-platform";
        var tokenEndpoint = $"{authority.TrimEnd('/')}/protocol/openid-connect/token";

        return Problem(
            statusCode: StatusCodes.Status410Gone,
            title: "Login endpoint removed",
            detail: $"Authenticate via Keycloak. Obtain a token from POST {tokenEndpoint} and send Authorization: Bearer <access_token> to the API.",
            type: "https://httpstatuses.com/410");
    }

    /// <summary>
    /// Returns the authenticated user profile from the Bearer token (BFF-style).
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(MeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public ActionResult<MeDto> Me()
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "A valid Bearer token is required.");
        }

        return Ok(MeDto.FromCurrentUser(currentUser));
    }

    /// <summary>
    /// Register a new user (legacy local account; prefer Keycloak in production).
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { error = "Email is required." });

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            return BadRequest(new { error = "Password must be at least 8 characters." });

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Name is required." });

        try
        {
            var user = await authService.RegisterAsync(request, cancellationToken);
            return CreatedAtAction(nameof(Me), user);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
