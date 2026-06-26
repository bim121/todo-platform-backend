using TodoPlatform.Domain.Common;
using TodoPlatform.Domain.Events;

namespace TodoPlatform.Domain.Entities;

public class User : Entity
{
    public const string ExternalPasswordPlaceholder = "keycloak:external";

    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? KeycloakSub { get; private set; }

    private User()
    {
    }

    public static User Register(string email, string passwordHash, string name)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        return new User
        {
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            Name = name.Trim()
        };
    }

    public static User CreateFromKeycloak(string keycloakSub, string email, string name)
    {
        if (string.IsNullOrWhiteSpace(keycloakSub))
            throw new ArgumentException("Keycloak subject is required.", nameof(keycloakSub));

        var user = new User
        {
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = ExternalPasswordPlaceholder,
            Name = name.Trim(),
            KeycloakSub = keycloakSub.Trim(),
        };

        user.RaiseDomainEvent(new UserRegisteredEvent(user.Id, user.Email, user.KeycloakSub));
        return user;
    }

    public void LinkKeycloakSubject(string keycloakSub)
    {
        if (string.IsNullOrWhiteSpace(keycloakSub))
            throw new ArgumentException("Keycloak subject is required.", nameof(keycloakSub));

        if (!string.IsNullOrWhiteSpace(KeycloakSub) && KeycloakSub != keycloakSub)
            throw new InvalidOperationException("User is already linked to a different Keycloak subject.");

        KeycloakSub = keycloakSub.Trim();
    }
}
