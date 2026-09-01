using TodoPlatform.Application.Services;

namespace TodoPlatform.Api.Auth;

public sealed class CurrentUserSyncMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IUserSyncService userSyncService)
    {
        if (ShouldSkip(context))
        {
            await next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated == true)
            await userSyncService.SyncCurrentUserAsync(context.RequestAborted);

        await next(context);
    }

    private static bool ShouldSkip(HttpContext context) =>
        context.Request.Path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase);
}
