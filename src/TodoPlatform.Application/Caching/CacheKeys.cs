namespace TodoPlatform.Application.Caching;

public static class CacheKeys
{
    public static string TodosByUser(Guid userId) => $"todos:user:{userId}";

    public static string TodoById(Guid todoId) => $"todo:{todoId}";
}
