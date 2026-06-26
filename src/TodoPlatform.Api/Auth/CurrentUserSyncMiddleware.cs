using TodoPlatform.Application.Services;

namespace TodoPlatform.Api.Auth;

public sealed class CurrentUserSyncMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IUserSyncService userSyncService)
    {
        if (context.User.Identity?.IsAuthenticated == true)
            await userSyncService.SyncCurrentUserAsync(context.RequestAborted);

        await next(context);
    }
}
