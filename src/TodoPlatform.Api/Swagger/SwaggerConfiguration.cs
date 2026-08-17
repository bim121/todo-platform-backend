using System.Reflection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using TodoPlatform.Api.Configuration;

namespace TodoPlatform.Api.Swagger;

public static class SwaggerConfiguration
{
    public const string BearerSchemeName = "bearerAuth";
    public const string OAuthSchemeName = "keycloak";

    public static IServiceCollection AddApiSwagger(this IServiceCollection services, IConfiguration configuration)
    {
        var assembly = typeof(Program).Assembly;
        var info = CreateOpenApiInfo(assembly);
        var authority = configuration
            .GetSection(KeycloakOptions.SectionName)
            .Get<KeycloakOptions>()?.Authority
            .TrimEnd('/') ?? "http://localhost:8180/realms/todo-platform";

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", info);

            var xmlFile = $"{assembly.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath);

            options.AddSecurityDefinition(BearerSchemeName, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Keycloak access token (paste from token endpoint or use OAuth2 below)."
            });

            options.AddSecurityDefinition(OAuthSchemeName, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Description = "Keycloak authorization code flow with PKCE (client todo-spa).",
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri($"{authority}/protocol/openid-connect/auth"),
                        TokenUrl = new Uri($"{authority}/protocol/openid-connect/token"),
                        Scopes = new Dictionary<string, string>
                        {
                            ["openid"] = "OpenID Connect",
                            ["profile"] = "User profile",
                            ["email"] = "User email"
                        }
                    }
                }
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(BearerSchemeName, document)] = [],
                [new OpenApiSecuritySchemeReference(OAuthSchemeName, document)] = ["openid", "profile", "email"]
            });

            options.OperationFilter<TenantIdHeaderOperationFilter>();
        });

        return services;
    }

    public static void UseApiSwaggerUi(this WebApplication app)
    {
        var info = CreateOpenApiInfo(typeof(Program).Assembly);
        var documentTitle = $"{info.Title} {info.Version}";
        var keycloak = app.Services.GetRequiredService<IOptions<KeycloakOptions>>().Value;
        var authority = keycloak.Authority.TrimEnd('/');

        app.UseSwaggerUI(options =>
        {
            options.DocumentTitle = documentTitle;
            options.SwaggerEndpoint("/swagger/v1/swagger.json", documentTitle);
            options.OAuthClientId("todo-spa");
            options.OAuthUsePkce();
            options.OAuthScopes("openid", "profile", "email");

            if (!string.IsNullOrWhiteSpace(authority))
            {
                options.OAuthConfigObject.AdditionalQueryStringParams = new Dictionary<string, string>
                {
                    ["kc_idp_hint"] = string.Empty
                };
            }
        });
    }

    private static OpenApiInfo CreateOpenApiInfo(Assembly assembly)
    {
        var title = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product
            ?? assembly.GetName().Name
            ?? "Todo Platform API";

        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString(3)
            ?? "1.0.0";

        var plusIndex = version.IndexOf('+', StringComparison.Ordinal);
        if (plusIndex >= 0)
            version = version[..plusIndex];

        return new OpenApiInfo
        {
            Title = title,
            Version = version,
            Description = "REST API for Todo Platform. Contract: contracts/openapi.yaml"
        };
    }
}
