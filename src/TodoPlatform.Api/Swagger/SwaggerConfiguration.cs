using System.Reflection;
using Microsoft.OpenApi;

namespace TodoPlatform.Api.Swagger;

public static class SwaggerConfiguration
{
    public static IServiceCollection AddApiSwagger(this IServiceCollection services)
    {
        var assembly = typeof(Program).Assembly;
        var info = CreateOpenApiInfo(assembly);

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", info);

            var xmlFile = $"{assembly.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath);
        });

        return services;
    }

    public static void UseApiSwaggerUi(this WebApplication app)
    {
        var info = CreateOpenApiInfo(typeof(Program).Assembly);
        var documentTitle = $"{info.Title} {info.Version}";

        app.UseSwaggerUI(options =>
        {
            options.DocumentTitle = documentTitle;
            options.SwaggerEndpoint("/swagger/v1/swagger.json", documentTitle);
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

        // Strip git hash suffix from informational version (e.g. 1.0.0+abc123)
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
