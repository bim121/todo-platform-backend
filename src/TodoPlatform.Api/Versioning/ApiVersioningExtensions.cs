namespace TodoPlatform.Api.Versioning;

public static class ApiVersioningExtensions
{
    public static IApplicationBuilder UseApiVersioning(this IApplicationBuilder app)
    {
        app.UseMiddleware<ApiVersionMiddleware>();
        return app;
    }

    public static IApplicationBuilder UseDeprecationHeaders(this IApplicationBuilder app)
    {
        app.UseMiddleware<DeprecationHeaderMiddleware>();
        return app;
    }

    public static string GetApiVersion(this HttpContext context) =>
        context.Items.TryGetValue(ApiVersionDefaults.HttpContextItemKey, out var version) && version is string value
            ? value
            : ApiVersionDefaults.DefaultVersion;
}
