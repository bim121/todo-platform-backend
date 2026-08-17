namespace TodoPlatform.Application.Caching;

public static class CacheKeys
{
    public static string TodosByUser(
        Guid tenantId,
        Guid userId,
        bool activeOnly = false,
        int? skip = null,
        int? take = null) =>
        $"todos:tenant:{tenantId}:user:{userId}:a{activeOnly}:s{skip?.ToString() ?? "-"}:t{take?.ToString() ?? "-"}";

    /// <summary>Prefix for RemoveByPrefixAsync — clears all filter variants for a tenant+user.</summary>
    public static string TodosByUserPrefix(Guid tenantId, Guid userId) =>
        $"todos:tenant:{tenantId}:user:{userId}";

    public static string TodoById(Guid tenantId, Guid todoId) => $"todo:tenant:{tenantId}:{todoId}";

    /// <summary>B-10.6 / B-11.8 — per-tenant, per-user todo aggregates.</summary>
    public static string TodoStatsByUser(Guid tenantId, Guid userId) =>
        $"stats:tenant:{tenantId}:user:{userId}";
}
