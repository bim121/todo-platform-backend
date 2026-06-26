using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Domain.Entities;

namespace TodoPlatform.Application.Services;

public sealed class AuthService(IUserRepository users, IPasswordHasher passwordHasher) : IAuthService
{
    public async Task<UserDto> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);

        if (await users.ExistsByEmailAsync(email, cancellationToken))
            throw new InvalidOperationException("Email is already registered.");

        var user = User.Register(email, passwordHasher.Hash(request.Password), request.Name);
        await users.AddAsync(user, cancellationToken);
        return UserDto.FromEntity(user);
    }

    private static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();
}
