namespace TodoPlatform.Application.Realtime;

/// <summary>SignalR group names — one group per tenant+user (B-13.3).</summary>
public static class TodoHubGroups
{
    public static string ForUser(Guid tenantId, Guid userId) =>
        $"tenant:{tenantId:D}:user:{userId:D}";
}
