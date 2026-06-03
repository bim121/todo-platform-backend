using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Domain.Entities;

namespace TodoPlatform.Application.Services;

public sealed class AuthService(IUserRepository users, IPasswordHasher passwordHasher) : IAuthService
{
    public async Task<AuthResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        var user = await users.GetByEmailAsync(email, cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            return null;

        return AuthResponse.FromUser(user, CreateMockToken(user.Id));
    }

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

    private static string CreateMockToken(Guid userId) =>
        $"mockToken={userId}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
}
