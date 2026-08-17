using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TodoPlatform.Application.Caching;
using TodoPlatform.Domain.Tenancy;
using TodoPlatform.Infrastructure.Caching;

namespace TodoPlatform.Infrastructure.Tests.Caching;

public sealed class MemoryCacheServiceTests
{
    [Fact]
    public async Task GetOrSetAsync_SecondCall_IsCacheHit()
    {
        var metrics = new CacheMetrics();
        var sut = CreateSut(metrics);
        var factoryCalls = 0;
        var key = CacheKeys.TodosByUser(WellKnownTenants.DefaultId, Guid.NewGuid());

        var first = await sut.GetOrSetAsync(
            key,
            _ =>
            {
                factoryCalls++;
                return Task.FromResult(new[] { "a", "b" });
            },
            TimeSpan.FromMinutes(5));

        var second = await sut.GetOrSetAsync(
            key,
            _ =>
            {
                factoryCalls++;
                return Task.FromResult(new[] { "should-not-run" });
            },
            TimeSpan.FromMinutes(5));

        Assert.Equal(first, second);
        Assert.Equal(1, factoryCalls);
        Assert.Equal(1, metrics.Misses);
        Assert.Equal(1, metrics.Hits);
    }

    [Fact]
    public async Task RemoveByPrefixAsync_RemovesMatchingKeys()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var tenantId = WellKnownTenants.DefaultId;
        var listKey = CacheKeys.TodosByUser(tenantId, userId);
        var todoKey = CacheKeys.TodoById(tenantId, Guid.NewGuid());

        await sut.GetOrSetAsync(listKey, _ => Task.FromResult("list"), TimeSpan.FromMinutes(1));
        await sut.GetOrSetAsync(todoKey, _ => Task.FromResult("todo"), TimeSpan.FromMinutes(1));

        await sut.RemoveByPrefixAsync(CacheKeys.TodosByUserPrefix(tenantId, userId));

        var factoryCalls = 0;
        await sut.GetOrSetAsync(
            listKey,
            _ =>
            {
                factoryCalls++;
                return Task.FromResult("list-rebuilt");
            },
            TimeSpan.FromMinutes(1));

        Assert.Equal(1, factoryCalls);

        factoryCalls = 0;
        await sut.GetOrSetAsync(
            todoKey,
            _ =>
            {
                factoryCalls++;
                return Task.FromResult("should-not");
            },
            TimeSpan.FromMinutes(1));
        Assert.Equal(0, factoryCalls);
    }

    [Fact]
    public async Task GetOrSetAsync_EmptyCollection_UsesShortTtlPath()
    {
        var sut = CreateSut();
        var key = CacheKeys.TodosByUser(WellKnownTenants.DefaultId, Guid.NewGuid());

        var result = await sut.GetOrSetAsync(
            key,
            _ => Task.FromResult((IReadOnlyList<string>)Array.Empty<string>()),
            TimeSpan.FromMinutes(5),
            emptyCollectionTtl: TimeSpan.FromSeconds(30));

        Assert.Empty(result);
    }

    private static MemoryCacheService CreateSut(CacheMetrics? metrics = null)
    {
        var opts = Options.Create(new MemoryDistributedCacheOptions());
        IDistributedCache cache = new MemoryDistributedCache(opts);
        return new MemoryCacheService(
            cache,
            metrics ?? new CacheMetrics(),
            NullLogger<MemoryCacheService>.Instance);
    }
}
