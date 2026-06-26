using System.Security.Claims;
using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Api.Auth;

public sealed class CurrentUserEnrichmentMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IUserRepository userRepository)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && !context.Items.ContainsKey(CurrentUserItemKeys.UserId))
        {
            var email = context.User.FindFirstValue(ClaimTypes.Email)
                ?? context.User.FindFirstValue("email")
                ?? context.User.FindFirstValue("preferred_username");

            if (!string.IsNullOrWhiteSpace(email))
            {
                var user = await userRepository.GetByEmailAsync(email, context.RequestAborted);
                if (user is not null)
                    context.Items[CurrentUserItemKeys.UserId] = user.Id;
            }
        }

        await next(context);
    }
}
