namespace TodoPlatform.Application.Diagnostics;

/// <summary>
/// Process-local slow EF query counter until OpenTelemetry (B-24).
/// </summary>
public sealed class SlowQueryMetrics
{
    private long _count;

    public long Count => Interlocked.Read(ref _count);

    public void RecordSlowQuery() => Interlocked.Increment(ref _count);
}
