using TodoPlatform.Application.Dtos;

namespace TodoPlatform.Application.Services;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
}
