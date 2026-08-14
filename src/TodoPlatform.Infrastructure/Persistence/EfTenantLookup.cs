using Microsoft.EntityFrameworkCore;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Domain.Entities;

namespace TodoPlatform.Infrastructure.Persistence;

public sealed class EfTenantLookup(AppDbContext db) : ITenantLookup
{
    public async Task<Tenant?> FindByIdOrSlugAsync(
        string idOrSlug,
        CancellationToken cancellationToken = default)
    {
        var value = idOrSlug.Trim();
        if (value.Length == 0)
            return null;

        if (Guid.TryParse(value, out var id))
        {
            return await db.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        }

        var slug = value.ToLowerInvariant();
        return await db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);
    }
}
