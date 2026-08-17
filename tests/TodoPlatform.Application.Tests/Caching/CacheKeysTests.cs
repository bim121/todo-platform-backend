using TodoPlatform.Application.Caching;
using TodoPlatform.Domain.Tenancy;

namespace TodoPlatform.Application.Tests.Caching;

public sealed class CacheKeysTests
{
    [Fact]
    public void TodosByUser_IncludesTenantAndIsPrefixedForInvalidation()
    {
        var tenantId = WellKnownTenants.AcmeId;
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var key = CacheKeys.TodosByUser(tenantId, userId, activeOnly: true, skip: 0, take: 20);
        var prefix = CacheKeys.TodosByUserPrefix(tenantId, userId);

        Assert.StartsWith($"todos:tenant:{tenantId}:user:{userId}", key, StringComparison.Ordinal);
        Assert.StartsWith(prefix, key, StringComparison.Ordinal);
        Assert.Contains(":aTrue:s0:t20", key, StringComparison.Ordinal);
    }

    [Fact]
    public void TodoById_And_Stats_AreTenantScoped()
    {
        var tenantId = WellKnownTenants.DefaultId;
        var id = Guid.Parse("22222222-2222-2222-2222-222222222222");

        Assert.Equal($"todo:tenant:{tenantId}:{id}", CacheKeys.TodoById(tenantId, id));
        Assert.Equal($"stats:tenant:{tenantId}:user:{id}", CacheKeys.TodoStatsByUser(tenantId, id));
    }
}
