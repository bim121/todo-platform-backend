using System.Security.Claims;
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

    public string? KeycloakSub =>
        httpContextAccessor.HttpContext?.User.FindFirstValue("sub");

    public string? Name
    {
        get
        {
            if (GetSyncedUser() is { } user)
                return user.Name;

            return httpContextAccessor.HttpContext?.User.FindFirstValue("name")
                ?? httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name);
        }
    }

    public IReadOnlyList<string> Roles =>
        httpContextAccessor.HttpContext?.User
            .FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()
        ?? [];

    public Guid UserId
    {
        get
        {
            var context = httpContextAccessor.HttpContext;
            if (context is null)
                return Guid.Empty;

            if (context.Items.TryGetValue(CurrentUserContextKeys.UserId, out var value) && value is Guid userId)
                return userId;

            if (GetSyncedUser() is { } user)
                return user.Id;

            var sub = context.User.FindFirstValue("sub");
            return Guid.TryParse(sub, out userId) ? userId : Guid.Empty;
        }
    }

    public bool IsInRole(string role) =>
        httpContextAccessor.HttpContext?.User.IsInRole(role) == true;

    private Domain.Entities.User? GetSyncedUser()
    {
        var context = httpContextAccessor.HttpContext;
        if (context?.Items.TryGetValue(CurrentUserContextKeys.SyncedUser, out var value) == true
            && value is Domain.Entities.User user)
        {
            return user;
        }

        return null;
    }
}
