using System.Security.Claims;
using TodoPlatform.Api.Middleware;

namespace TodoPlatform.Api.Realtime;

/// <summary>Resolves tenant token from header, query, or JWT for SignalR negotiate (B-13.3).</summary>
public static class HubTenantTokenReader
{
    public const string QueryName = "tenant";

    public static string? Read(HttpContext? httpContext)
    {
        if (httpContext is null)
            return null;

        if (httpContext.Request.Headers.TryGetValue(TenantResolutionMiddleware.HeaderName, out var headerValues))
        {
            var header = headerValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(header))
                return header.Trim();
        }

        if (httpContext.Request.Query.TryGetValue(QueryName, out var queryValues))
        {
            var query = queryValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(query))
                return query.Trim();
        }

        var claim = httpContext.User.FindFirstValue(TenantResolutionMiddleware.JwtClaim)
            ?? httpContext.User.FindFirstValue("tenantId");
        return string.IsNullOrWhiteSpace(claim) ? null : claim.Trim();
    }
}
