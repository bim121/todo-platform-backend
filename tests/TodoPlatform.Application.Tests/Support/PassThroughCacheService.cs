using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Application.Tests.Support;

/// <summary>Test double: always runs factory (no real cache).</summary>
public sealed class PassThroughCacheService : ICacheService
{
    public int GetOrSetCalls { get; private set; }
    public int RemoveCalls { get; private set; }
    public int RemoveByPrefixCalls { get; private set; }
    public List<string> RemovedKeys { get; } = [];
    public List<string> RemovedPrefixes { get; } = [];

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default,
        TimeSpan? emptyCollectionTtl = null)
    {
        GetOrSetCalls++;
        return await factory(cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        RemoveCalls++;
        RemovedKeys.Add(key);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        RemoveByPrefixCalls++;
        RemovedPrefixes.Add(prefix);
        return Task.CompletedTask;
    }
}
