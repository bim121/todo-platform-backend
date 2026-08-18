using TodoPlatform.Application.Dtos;

namespace TodoPlatform.Application.Interfaces;

public interface ITenantAdminReadStore
{
    Task<IReadOnlyList<TenantAdminDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<TenantAdminDto?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
