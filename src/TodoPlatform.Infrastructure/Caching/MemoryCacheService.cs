using System.Collections;
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using TodoPlatform.Application.Caching;
using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Infrastructure.Caching;

/// <summary>
/// In-memory ICacheService for tests and local runs without Redis (Cache:UseMemory / Database:UseInMemory).
/// </summary>
public sealed class MemoryCacheService(
    IDistributedCache distributedCache,
    CacheMetrics metrics,
    ILogger<MemoryCacheService> logger) : ICacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly ConcurrentDictionary<string, byte> _keys = new(StringComparer.Ordinal);

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default,
        TimeSpan? emptyCollectionTtl = null)
    {
        var cached = await distributedCache.GetStringAsync(key, cancellationToken);
        if (cached is not null)
        {
            metrics.RecordHit();
            logger.LogDebug("Cache HIT for key {CacheKey}", key);
            return JsonSerializer.Deserialize<T>(cached, SerializerOptions)!;
        }

        metrics.RecordMiss();
        logger.LogDebug("Cache MISS for key {CacheKey}", key);
        var value = await factory(cancellationToken);
        var effectiveTtl = ResolveTtl(value, ttl, emptyCollectionTtl);
        var payload = JsonSerializer.Serialize(value, SerializerOptions);
        await distributedCache.SetStringAsync(
            key,
            payload,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = effectiveTtl },
            cancellationToken);
        _keys[key] = 0;
        return value;
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await distributedCache.RemoveAsync(key, cancellationToken);
        _keys.TryRemove(key, out _);
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        foreach (var key in _keys.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)))
            await RemoveAsync(key, cancellationToken);

        logger.LogDebug("Cache RemoveByPrefix completed for prefix {Prefix}", prefix);
    }

    private static TimeSpan ResolveTtl<T>(T value, TimeSpan ttl, TimeSpan? emptyCollectionTtl)
    {
        if (emptyCollectionTtl is null)
            return ttl;

        if (value is ICollection { Count: 0 })
            return emptyCollectionTtl.Value;

        return ttl;
    }
}
