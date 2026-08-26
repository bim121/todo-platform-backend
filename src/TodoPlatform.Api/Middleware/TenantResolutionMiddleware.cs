using System.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TodoPlatform.Application.Exceptions;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Tenancy;
using TodoPlatform.Infrastructure.Persistence;
using TodoPlatform.Infrastructure.Tenancy;

namespace TodoPlatform.Api.Middleware;

/// <summary>
/// B-11.4 — resolves tenant from <c>X-Tenant-Id</c> (UUID or slug) or JWT <c>tenant_id</c>.
/// <para>
/// Pipeline order (see Program.cs): after <c>UseAuthentication</c> (JWT claims available),
/// before <c>UseCurrentUserSync</c> / <c>UseAuthorization</c> so RLS SET applies to user lookup.
/// Unauthenticated callers skip the required-header check (401 comes from <c>[Authorize]</c>).
/// Health and Swagger are excluded.
/// </para>
/// </summary>
public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Tenant-Id";
    public const string JwtClaim = "tenant_id";

    public async Task InvokeAsync(
        HttpContext context,
        ITenantContext tenantContext,
        ITenantLookup tenantLookup,
        AppDbContext db)
    {
        if (ShouldSkip(context))
        {
            await next(context);
            return;
        }

        var raw = ReadTenantToken(context);
        if (string.IsNullOrWhiteSpace(raw))
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                throw new ValidationException(
                    "Tenant is required.",
                    new Dictionary<string, string[]>
                    {
                        [HeaderName] =
                        [
                            "Header 'X-Tenant-Id' (tenant UUID or slug) is required when authenticated, "
                            + "or provide JWT claim 'tenant_id'."
                        ]
                    });
            }

            await next(context);
            return;
        }

        var tenant = await tenantLookup.FindByIdOrSlugAsync(raw, context.RequestAborted);
        if (tenant is null || !tenant.IsActive)
        {
            throw new NotFoundException($"Tenant '{raw}' was not found or is inactive.");
        }

        tenantContext.Set(tenant.Id, tenant.Slug, tenant.SchemaName);

        if (db.Database.IsRelational())
        {
            var connection = db.Database.GetDbConnection();
            if (connection.State == ConnectionState.Open)
                await TenantSession.ApplyAsync(connection, tenant.Id, tenant.SchemaName, context.RequestAborted);
        }

        await next(context);
    }

    private static bool ShouldSkip(HttpContext context)
    {
        var path = context.Request.Path;
        return path.StartsWithSegments("/health")
            || path.StartsWithSegments("/swagger")
            || path.StartsWithSegments("/api/health")
            || path == "/"
            || HttpMethods.IsOptions(context.Request.Method);
    }

    private static string? ReadTenantToken(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var headerValues))
        {
            var header = headerValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(header))
                return header.Trim();
        }

        var claim = context.User.FindFirstValue(JwtClaim)
            ?? context.User.FindFirstValue("tenantId");
        return string.IsNullOrWhiteSpace(claim) ? null : claim.Trim();
    }
}
