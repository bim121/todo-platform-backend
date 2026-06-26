using TodoPlatform.Application.Dtos;
using TodoPlatform.Domain.Entities;

namespace TodoPlatform.Application.Services;

public interface IUserSyncService
{
    Task<User?> SyncCurrentUserAsync(CancellationToken cancellationToken = default);
}
