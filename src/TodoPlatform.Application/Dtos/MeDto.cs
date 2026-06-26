namespace TodoPlatform.Application.Dtos;

public sealed record MeDto(
    Guid Id,
    string Email,
    string Name,
    string KeycloakSub,
    IReadOnlyList<string> Roles)
{
    public static MeDto FromCurrentUser(Services.ICurrentUserService currentUser) =>
        new(
            currentUser.UserId,
            currentUser.Email ?? string.Empty,
            currentUser.Name ?? string.Empty,
            currentUser.KeycloakSub ?? string.Empty,
            currentUser.Roles);
}
