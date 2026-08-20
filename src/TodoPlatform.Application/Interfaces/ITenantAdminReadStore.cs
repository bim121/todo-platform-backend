using TodoPlatform.Application.Dtos;

namespace TodoPlatform.Application.Interfaces;

public interface ITenantAdminReadStore
{
    Task<PagedResult<TenantAdminDto>> ListAsync(
        TenantAdminListFilter filter,
        CancellationToken cancellationToken = default);

    Task<TenantAdminDto?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

/// <summary>Admin tenant list pagination + optional track/status filters (B-12.4).</summary>
public sealed record TenantAdminListFilter(
    int Skip = 0,
    int Take = 20,
    string? Track = null,
    string? Status = null);
