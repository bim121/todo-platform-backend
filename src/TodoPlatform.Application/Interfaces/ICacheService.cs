using TodoPlatform.Application.Caching;

namespace TodoPlatform.Application.Interfaces;

public interface ICacheService
{
    /// <param name="ttl">TTL for non-empty values.</param>
    /// <param name="emptyCollectionTtl">
    /// If <paramref name="factory"/> returns an empty <see cref="IReadOnlyCollection{T}"/>,
    /// use this shorter TTL (anti-cache-forever-empty). Defaults to <paramref name="ttl"/>.
    /// </param>
    Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default,
        TimeSpan? emptyCollectionTtl = null);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
}
