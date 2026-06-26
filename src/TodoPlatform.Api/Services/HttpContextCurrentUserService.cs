using System.Security.Claims;
using TodoPlatform.Api.Auth;
using TodoPlatform.Application.Services;

namespace TodoPlatform.Api.Services;

public sealed class HttpContextCurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public string? Email =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email)
        ?? httpContextAccessor.HttpContext?.User.FindFirstValue("email")
        ?? httpContextAccessor.HttpContext?.User.FindFirstValue("preferred_username");

    public Guid UserId
    {
        get
        {
            var context = httpContextAccessor.HttpContext;
            if (context is null)
                return Guid.Empty;

            if (context.Items.TryGetValue(CurrentUserItemKeys.UserId, out var value) && value is Guid userId)
                return userId;

            var sub = context.User.FindFirstValue("sub");
            return Guid.TryParse(sub, out userId) ? userId : Guid.Empty;
        }
    }

    public bool IsInRole(string role) =>
        httpContextAccessor.HttpContext?.User.IsInRole(role) == true;
}
