using Microsoft.AspNetCore.SignalR;
using TodoPlatform.Api.Hubs;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Realtime;

namespace TodoPlatform.Api.Realtime;

/// <summary>B-13.4 — SignalR push scoped to <c>tenant:{tid}:user:{uid}</c>.</summary>
public sealed class SignalRTodoRealtimeNotifier(IHubContext<TodoHub, ITodoHubClient> hub)
    : ITodoRealtimeNotifier
{
    public Task NotifyCreatedAsync(
        Guid tenantId,
        Guid userId,
        TodoRealtimeMessage message,
        CancellationToken cancellationToken = default) =>
        hub.Clients
            .Group(TodoHubGroups.ForUser(tenantId, userId))
            .TodoCreated(message);

    public Task NotifyUpdatedAsync(
        Guid tenantId,
        Guid userId,
        TodoRealtimeMessage message,
        CancellationToken cancellationToken = default) =>
        hub.Clients
            .Group(TodoHubGroups.ForUser(tenantId, userId))
            .TodoUpdated(message);

    public Task NotifyDeletedAsync(
        Guid tenantId,
        Guid userId,
        TodoRealtimeMessage message,
        CancellationToken cancellationToken = default) =>
        hub.Clients
            .Group(TodoHubGroups.ForUser(tenantId, userId))
            .TodoDeleted(message);
}
