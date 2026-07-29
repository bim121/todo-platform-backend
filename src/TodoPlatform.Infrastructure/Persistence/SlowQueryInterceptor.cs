using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using TodoPlatform.Application.Diagnostics;

namespace TodoPlatform.Infrastructure.Persistence;

/// <summary>
/// Logs EF commands slower than the threshold with Serilog property <c>SlowQuery</c> (B-09.7).
/// </summary>
public sealed class SlowQueryInterceptor(
    ILogger<SlowQueryInterceptor> logger,
    SlowQueryMetrics metrics) : DbCommandInterceptor
{
    public static readonly TimeSpan Threshold = TimeSpan.FromMilliseconds(200);

    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        Observe(command, eventData.Duration);
        return base.ReaderExecuted(command, eventData, result);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        Observe(command, eventData.Duration);
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        Observe(command, eventData.Duration);
        return base.NonQueryExecuted(command, eventData, result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        Observe(command, eventData.Duration);
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override object? ScalarExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result)
    {
        Observe(command, eventData.Duration);
        return base.ScalarExecuted(command, eventData, result);
    }

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken = default)
    {
        Observe(command, eventData.Duration);
        return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    private void Observe(DbCommand command, TimeSpan duration)
    {
        if (duration < Threshold)
            return;

        metrics.RecordSlowQuery();

        logger.LogWarning(
            "EF command exceeded threshold {SlowQuery} DurationMs={DurationMs} CommandText={CommandText}",
            true,
            (int)duration.TotalMilliseconds,
            Truncate(command.CommandText));
    }

    private static string Truncate(string? sql, int max = 500)
    {
        if (string.IsNullOrEmpty(sql))
            return string.Empty;

        return sql.Length <= max ? sql : sql[..max] + "…";
    }
}
