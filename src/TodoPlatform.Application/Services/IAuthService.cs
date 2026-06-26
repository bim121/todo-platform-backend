using TodoPlatform.Application.Dtos;

namespace TodoPlatform.Application.Services;

public interface IAuthService
{
    Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
}
