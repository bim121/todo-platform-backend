using TodoPlatform.Domain.Entities;

namespace TodoPlatform.Application.Dtos;

public sealed record UserDto(Guid Id, string Email, string Name)
{
    public static UserDto FromEntity(User user) =>
        new(user.Id, user.Email, user.Name);
}
