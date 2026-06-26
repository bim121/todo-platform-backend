using TodoPlatform.Domain.Entities;

namespace TodoPlatform.Application.Dtos;

public sealed record AuthResponse(string Token, UserDto User)
{
    public static AuthResponse FromUser(User user, string token) =>
        new(token, UserDto.FromEntity(user));
}
