using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Realtime;
using TodoPlatform.Application.Services;
using TodoPlatform.Application.Tenancy;
using TodoPlatform.Api.Realtime;

namespace TodoPlatform.Api.Hubs;

/// <summary>
/// B-13.1–3 — live todo updates hub. Clients join <c>tenant:{tid}:user:{uid}</c> on connect.
/// </summary>
[Authorize]
public sealed class TodoHub(
    ITenantLookup tenantLookup,
    ITenantContext tenantContext,
    IUserSyncService userSync,
    ILogger<TodoHub> logger) : Hub<ITodoHubClient>
{
    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var tenantToken = HubTenantTokenReader.Read(httpContext);
        if (string.IsNullOrWhiteSpace(tenantToken))
        {
            logger.LogWarning(
                "SignalR connection {ConnectionId} rejected: tenant header/query/claim missing",
                Context.ConnectionId);
            Context.Abort();
            return;
        }

        var tenant = await tenantLookup.FindByIdOrSlugAsync(tenantToken, Context.ConnectionAborted);
        if (tenant is null || !tenant.IsActive)
        {
            logger.LogWarning(
                "SignalR connection {ConnectionId} rejected: tenant '{TenantToken}' not found or inactive",
                Context.ConnectionId,
                tenantToken);
            Context.Abort();
            return;
        }

        tenantContext.Set(tenant.Id, tenant.Slug, tenant.SchemaName);

        var user = await userSync.SyncCurrentUserAsync(Context.ConnectionAborted);
        if (user is null)
        {
            logger.LogWarning(
                "SignalR connection {ConnectionId} rejected: could not resolve synced user",
                Context.ConnectionId);
            Context.Abort();
            return;
        }

        var group = TodoHubGroups.ForUser(tenant.Id, user.Id);
        await Groups.AddToGroupAsync(Context.ConnectionId, group, Context.ConnectionAborted);

        logger.LogInformation(
            "SignalR connected connection={ConnectionId} tenant={TenantId} user={UserId} group={Group}",
            Context.ConnectionId,
            tenant.Id,
            user.Id,
            group);

        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception is not null)
        {
            logger.LogWarning(
                exception,
                "SignalR disconnected with error connection={ConnectionId}",
                Context.ConnectionId);
        }
        else
        {
            logger.LogInformation(
                "SignalR disconnected connection={ConnectionId}",
                Context.ConnectionId);
        }

        return base.OnDisconnectedAsync(exception);
    }
}
