using TodoPlatform.Application.Dtos;

namespace TodoPlatform.Application.Interfaces;

public interface ISystemStatsReadStore
{
    Task<SystemStatsDto> GetAsync(CancellationToken cancellationToken = default);
}
