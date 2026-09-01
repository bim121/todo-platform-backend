using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TodoPlatform.Api.Auth;
using TodoPlatform.Api.Configuration;
using TodoPlatform.Api.Services;
using TodoPlatform.Application.Services;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Api.Extensions;

public static class AuthExtensions
{
    public static IServiceCollection AddApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<KeycloakOptions>(configuration.GetSection(KeycloakOptions.SectionName));
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, HttpContextCurrentUserService>();
        services.AddTransient<IClaimsTransformation, KeycloakRealmRolesClaimsTransformation>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireAdmin", policy => policy.RequireRole("admin"));
            options.AddPolicy("RequireUser", policy => policy.RequireRole("user"));
        });

        if (environment.IsEnvironment("Testing"))
        {
            services.AddAuthentication(TestAuthHandler.AuthenticationSchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.AuthenticationSchemeName,
                    _ => { });
            return services;
        }

        var keycloak = configuration.GetSection(KeycloakOptions.SectionName).Get<KeycloakOptions>()
            ?? throw new InvalidOperationException(
                $"Configuration section '{KeycloakOptions.SectionName}' is required.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = keycloak.Authority;
                options.Audience = keycloak.Audience;
                options.RequireHttpsMetadata = keycloak.RequireHttpsMetadata;
                options.MetadataAddress = string.IsNullOrWhiteSpace(keycloak.MetadataAddress)
                    ? $"{keycloak.Authority.TrimEnd('/')}/.well-known/openid-configuration"
                    : keycloak.MetadataAddress;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidAudience = keycloak.Audience,
                    ValidateIssuer = true,
                    ValidIssuer = keycloak.Authority.TrimEnd('/'),
                    RoleClaimType = ClaimTypes.Role,
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // B-13.2 — browsers pass JWT on WebSocket via query string.
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken)
                            && path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                    OnChallenge = WriteAuthProblemDetailsAsync,
                    OnForbidden = WriteForbiddenProblemDetailsAsync,
                };
            });

        return services;
    }

    public static IApplicationBuilder UseCurrentUserSync(this IApplicationBuilder app) =>
        app.UseMiddleware<CurrentUserSyncMiddleware>();

    private static async Task WriteAuthProblemDetailsAsync(JwtBearerChallengeContext context)
    {
        context.HandleResponse();

        if (context.Response.HasStarted)
            return;

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Detail = "A valid Bearer token is required.",
            Type = "https://httpstatuses.com/401",
            Instance = context.Request.Path,
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }

    private static async Task WriteForbiddenProblemDetailsAsync(ForbiddenContext context)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Forbidden",
            Detail = "You do not have permission to access this resource.",
            Type = "https://httpstatuses.com/403",
            Instance = context.Request.Path,
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}

/// <summary>
/// Test authentication for integration tests. Send <c>Authorization: Bearer test</c>.
/// Optional headers: <c>X-Test-User-Email</c>, <c>X-Test-User-Roles</c> (comma-separated).
/// </summary>
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string AuthenticationSchemeName = "Test";
    public const string TestToken = "test";
    public const string UserEmailHeader = "X-Test-User-Email";
    public const string UserRolesHeader = "X-Test-User-Roles";
    public const string UserSubHeader = "X-Test-User-Sub";
    public const string TenantClaimHeader = "X-Test-Tenant-Claim";
    public const string DefaultTestSub = "11111111-1111-1111-1111-111111111111";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = ReadBearerOrHubQueryToken();
        if (token is null)
            return Task.FromResult(AuthenticateResult.NoResult());

        if (!string.Equals(token, TestToken, StringComparison.Ordinal))
            return Task.FromResult(AuthenticateResult.Fail("Invalid test bearer token."));

        var email = Request.Headers[UserEmailHeader].FirstOrDefault() ?? DbSeeder.TestEmail;
        var sub = Request.Headers[UserSubHeader].FirstOrDefault() ?? DefaultTestSub;
        var roles = Request.Headers[UserRolesHeader].FirstOrDefault()?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? ["user"];
        var tenantClaim = Request.Headers[TenantClaimHeader].FirstOrDefault();

        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email),
            new("email", email),
            new("preferred_username", email),
            new("sub", sub),
            new(ClaimTypes.Name, "Test User"),
            new("name", "Test User"),
        };

        if (!string.IsNullOrWhiteSpace(tenantClaim))
            claims.Add(new Claim("tenant_id", tenantClaim));

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, AuthenticationSchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationSchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private string? ReadBearerOrHubQueryToken()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return authorization["Bearer ".Length..].Trim();

        if (!Request.Path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase))
            return null;

        return Request.Query.TryGetValue("access_token", out var values)
            ? values.FirstOrDefault()?.Trim()
            : null;
    }
}
