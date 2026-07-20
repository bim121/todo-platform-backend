namespace TodoPlatform.Application.Caching;

public static class CacheKeys
{
    public static string TodosByUser(
        Guid userId,
        bool activeOnly = false,
        int? skip = null,
        int? take = null) =>
        $"todos:user:{userId}:a{activeOnly}:s{skip?.ToString() ?? "-"}:t{take?.ToString() ?? "-"}";

    /// <summary>Prefix for RemoveByPrefixAsync — clears all filter variants for a user.</summary>
    public static string TodosByUserPrefix(Guid userId) => $"todos:user:{userId}";

    public static string TodoById(Guid todoId) => $"todo:{todoId}";
}
