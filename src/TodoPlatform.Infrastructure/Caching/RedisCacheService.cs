using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Infrastructure.Caching;

public sealed class RedisCacheService(
    IDistributedCache distributedCache,
    IConnectionMultiplexer connectionMultiplexer,
    ILogger<RedisCacheService> logger) : ICacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        var cached = await distributedCache.GetStringAsync(key, cancellationToken);
        if (cached is not null)
        {
            logger.LogDebug("Cache HIT for key {CacheKey}", key);
            return JsonSerializer.Deserialize<T>(cached, SerializerOptions)!;
        }

        logger.LogDebug("Cache MISS for key {CacheKey}", key);
        var value = await factory(cancellationToken);
        var payload = JsonSerializer.Serialize(value, SerializerOptions);
        await distributedCache.SetStringAsync(
            key,
            payload,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            cancellationToken);

        return value;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        distributedCache.RemoveAsync(key, cancellationToken);

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var pattern = $"{DependencyInjection.RedisInstanceName}{prefix}*";
        var server = GetServer();
        var db = connectionMultiplexer.GetDatabase();

        await foreach (var key in server.KeysAsync(pattern: pattern).WithCancellation(cancellationToken))
            await db.KeyDeleteAsync(key);

        logger.LogDebug("Cache RemoveByPrefix completed for prefix {Prefix}", prefix);
    }

    private IServer GetServer()
    {
        var endpoints = connectionMultiplexer.GetEndPoints();
        if (endpoints.Length == 0)
            throw new InvalidOperationException("Redis has no endpoints configured.");

        return connectionMultiplexer.GetServer(endpoints[0]);
    }
}
