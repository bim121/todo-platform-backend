using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TodoPlatform.Application.Common;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Services;
using TodoPlatform.Domain.Entities;

namespace TodoPlatform.Infrastructure.Security;

public sealed class UserSyncService(
    IHttpContextAccessor httpContextAccessor,
    IUserRepository users,
    IDomainEventDispatcher domainEventDispatcher) : IUserSyncService
{
    public async Task<User?> SyncCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var context = httpContextAccessor.HttpContext;
        if (context?.User.Identity?.IsAuthenticated != true)
            return null;

        if (context.Items.TryGetValue(CurrentUserContextKeys.SyncedUser, out var cached) && cached is User cachedUser)
            return cachedUser;

        var keycloakSub = context.User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(keycloakSub))
            return null;

        var email = ResolveEmail(context.User);
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var name = ResolveName(context.User, email);

        var user = await users.GetByKeycloakSubAsync(keycloakSub, cancellationToken);
        if (user is null)
        {
            user = await users.GetByEmailAsync(email, cancellationToken);
            if (user is not null)
            {
                if (string.IsNullOrWhiteSpace(user.KeycloakSub))
                    user.LinkKeycloakSubject(keycloakSub);

                await users.UpdateAsync(user, cancellationToken);
            }
            else
            {
                user = User.CreateFromKeycloak(keycloakSub, email, name);
                await users.AddAsync(user, cancellationToken);

                if (user.DomainEvents.Count > 0)
                {
                    await domainEventDispatcher.DispatchEventsAsync(user.DomainEvents, cancellationToken);
                    user.ClearDomainEvents();
                }
            }
        }

        context.Items[CurrentUserContextKeys.SyncedUser] = user;
        context.Items[CurrentUserContextKeys.UserId] = user.Id;
        return user;
    }

    private static string? ResolveEmail(ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.Email)
        ?? principal.FindFirstValue("email")
        ?? principal.FindFirstValue("preferred_username");

    private static string ResolveName(ClaimsPrincipal principal, string email)
    {
        var name = principal.FindFirstValue("name")
            ?? principal.FindFirstValue(ClaimTypes.Name);

        if (!string.IsNullOrWhiteSpace(name))
            return name.Trim();

        var givenName = principal.FindFirstValue("given_name");
        var familyName = principal.FindFirstValue("family_name");
        if (!string.IsNullOrWhiteSpace(givenName) || !string.IsNullOrWhiteSpace(familyName))
            return $"{givenName} {familyName}".Trim();

        return email.Split('@')[0];
    }
}
