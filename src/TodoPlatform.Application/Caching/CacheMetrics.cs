using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Application.Caching;

/// <summary>
/// Lightweight counters until OpenTelemetry (B-24). Process-local; resets on restart.
/// </summary>
public sealed class CacheMetrics
{
    private long _hits;
    private long _misses;

    public long Hits => Interlocked.Read(ref _hits);
    public long Misses => Interlocked.Read(ref _misses);

    public void RecordHit() => Interlocked.Increment(ref _hits);

    public void RecordMiss() => Interlocked.Increment(ref _misses);
}

public static class CacheTtl
{
    public static readonly TimeSpan TodosList = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan TodosListEmpty = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan TodoById = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan TodoStats = TimeSpan.FromMinutes(1);
}
