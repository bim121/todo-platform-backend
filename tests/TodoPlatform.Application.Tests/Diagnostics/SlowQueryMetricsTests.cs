using TodoPlatform.Application.Diagnostics;

namespace TodoPlatform.Application.Tests.Diagnostics;

public sealed class SlowQueryMetricsTests
{
    [Fact]
    public void RecordSlowQuery_IncrementsCount()
    {
        var metrics = new SlowQueryMetrics();
        Assert.Equal(0, metrics.Count);

        metrics.RecordSlowQuery();
        metrics.RecordSlowQuery();

        Assert.Equal(2, metrics.Count);
    }
}
