using TodoPlatform.Application.Exceptions;

namespace TodoPlatform.Api.Versioning;

public sealed class ApiVersionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var requestedVersion = context.Request.Headers[ApiVersionDefaults.HeaderName].FirstOrDefault();
        var version = string.IsNullOrWhiteSpace(requestedVersion)
            ? ApiVersionDefaults.DefaultVersion
            : requestedVersion.Trim();

        if (!ApiVersionDefaults.SupportedVersions.Contains(version))
        {
            throw ValidationException.ForField(
                ApiVersionDefaults.HeaderName,
                $"API version '{version}' is not supported. Supported versions: {ApiVersionDefaults.DefaultVersion}.");
        }

        context.Items[ApiVersionDefaults.HttpContextItemKey] = version;
        await next(context);
    }
}
