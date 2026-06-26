namespace TodoPlatform.Application.Services;

public interface ICurrentUserService
{
    Guid UserId { get; }

    string? Email { get; }

    string? Name { get; }

    string? KeycloakSub { get; }

    IReadOnlyList<string> Roles { get; }

    bool IsAuthenticated { get; }

    bool IsInRole(string role);
}
