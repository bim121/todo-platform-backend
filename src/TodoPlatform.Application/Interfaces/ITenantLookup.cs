using TodoPlatform.Domain.Entities;

namespace TodoPlatform.Application.Interfaces;

public interface ITenantLookup
{
    Task<Tenant?> FindByIdOrSlugAsync(string idOrSlug, CancellationToken cancellationToken = default);
}
