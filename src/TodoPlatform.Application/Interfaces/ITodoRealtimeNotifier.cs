namespace TodoPlatform.Application.Interfaces;

/// <summary>Pushes live todo updates to SignalR groups (B-13.4).</summary>
public interface ITodoRealtimeNotifier
{
    Task NotifyCreatedAsync(
        Guid tenantId,
        Guid userId,
        TodoPlatform.Application.Realtime.TodoRealtimeMessage message,
        CancellationToken cancellationToken = default);

    Task NotifyUpdatedAsync(
        Guid tenantId,
        Guid userId,
        TodoPlatform.Application.Realtime.TodoRealtimeMessage message,
        CancellationToken cancellationToken = default);

    Task NotifyDeletedAsync(
        Guid tenantId,
        Guid userId,
        TodoPlatform.Application.Realtime.TodoRealtimeMessage message,
        CancellationToken cancellationToken = default);
}
