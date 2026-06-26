using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace TodoPlatform.Api.Auth;

/// <summary>
/// Converts empty 401/403 responses from the authorization middleware into RFC 7807 ProblemDetails.
/// </summary>
public sealed class AuthorizationProblemDetailsMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        if (context.Response.HasStarted)
            return;

        if (context.Response.StatusCode is not (StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden))
            return;

        if (!string.IsNullOrEmpty(context.Response.ContentType))
            return;

        var (title, detail) = context.Response.StatusCode == StatusCodes.Status401Unauthorized
            ? ("Unauthorized", "A valid Bearer token is required.")
            : ("Forbidden", "You do not have permission to access this resource.");

        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = context.Response.StatusCode,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.com/{context.Response.StatusCode}",
            Instance = context.Request.Path,
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
