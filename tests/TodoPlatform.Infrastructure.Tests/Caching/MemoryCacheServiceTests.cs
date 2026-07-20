using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TodoPlatform.Application.Caching;
using TodoPlatform.Infrastructure.Caching;

namespace TodoPlatform.Infrastructure.Tests.Caching;

public sealed class MemoryCacheServiceTests
{
    [Fact]
    public async Task GetOrSetAsync_SecondCall_IsCacheHit()
    {
        var sut = CreateSut();
        var factoryCalls = 0;
        var key = CacheKeys.TodosByUser(Guid.NewGuid());

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
    }

    [Fact]
    public async Task RemoveByPrefixAsync_RemovesMatchingKeys()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var listKey = CacheKeys.TodosByUser(userId);
        var todoKey = CacheKeys.TodoById(Guid.NewGuid());

        await sut.GetOrSetAsync(listKey, _ => Task.FromResult("list"), TimeSpan.FromMinutes(1));
        await sut.GetOrSetAsync(todoKey, _ => Task.FromResult("todo"), TimeSpan.FromMinutes(1));

        await sut.RemoveByPrefixAsync("todos:user:");

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

    private static MemoryCacheService CreateSut()
    {
        var opts = Options.Create(new MemoryDistributedCacheOptions());
        IDistributedCache cache = new MemoryDistributedCache(opts);
        return new MemoryCacheService(cache, NullLogger<MemoryCacheService>.Instance);
    }
}
