namespace TodoPlatform.Api.Versioning;

public sealed class DeprecationHeaderMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var deprecation = context.GetEndpoint()?.Metadata.GetMetadata<DeprecatedEndpointAttribute>();
        if (deprecation is not null)
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers["Deprecation"] = "true";
                context.Response.Headers["Sunset"] = deprecation.Sunset;
                return Task.CompletedTask;
            });
        }

        await next(context);
    }
}
