using System.Text;
using Dapper;
using TodoPlatform.Application.Mapping;

namespace TodoPlatform.Infrastructure.Persistence;

/// <summary>
/// Builds parameterized COUNT + page SQL for todo filters.
/// Only whitelisted predicates are appended — never concatenate raw user column names.
/// </summary>
public static class TodoFilterSqlBuilder
{
    public const string SelectListColumns = """
        "Id",
        "Title",
        "Completed",
        "UserId",
        CASE "Status"
            WHEN 'Todo' THEN 'todo'
            WHEN 'InProgress' THEN 'in_progress'
            WHEN 'Done' THEN 'done'
            ELSE lower("Status"::text)
        END AS "Status",
        CASE "Priority"
            WHEN 'Low' THEN 'low'
            WHEN 'Medium' THEN 'medium'
            WHEN 'High' THEN 'high'
            ELSE lower("Priority"::text)
        END AS "Priority"
        """;

    public sealed record BuiltQuery(string CountSql, string PageSql, DynamicParameters Parameters);

    public static BuiltQuery Build(
        Guid userId,
        string? statusApi,
        string? priorityApi,
        bool? completed,
        string? search,
        int skip,
        int take)
    {
        var where = new StringBuilder("""WHERE "UserId" = @UserId""");
        var parameters = new DynamicParameters();
        parameters.Add("UserId", userId);
        parameters.Add("Skip", skip);
        parameters.Add("Take", take);

        if (!string.IsNullOrWhiteSpace(statusApi)
            && TodoContractMapper.TryParseStatus(statusApi, out var status))
        {
            where.Append(""" AND "Status" = @Status""");
            parameters.Add("Status", TodoContractMapper.ToDbStatusName(status));
        }

        if (!string.IsNullOrWhiteSpace(priorityApi)
            && TodoContractMapper.TryParsePriority(priorityApi, out var priority))
        {
            where.Append(""" AND "Priority" = @Priority""");
            parameters.Add("Priority", TodoContractMapper.ToDbPriorityName(priority));
        }

        if (completed.HasValue)
        {
            where.Append(""" AND "Completed" = @Completed""");
            parameters.Add("Completed", completed.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Append(""" AND "Title" ILIKE @Search ESCAPE '\'""");
            parameters.Add("Search", $"%{EscapeLike(search.Trim())}%");
        }

        var whereSql = where.ToString();
        var countSql = $"""SELECT COUNT(*)::int FROM todos {whereSql};""";
        var pageSql = $"""
            SELECT {SelectListColumns}
            FROM todos
            {whereSql}
            ORDER BY "Id"
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
            """;

        return new BuiltQuery(countSql, pageSql, parameters);
    }

    /// <summary>Reject unknown filter keys (for unit tests / future dynamic bags).</summary>
    public static bool IsAllowedFilterKey(string key) =>
        key.Equals("status", StringComparison.OrdinalIgnoreCase)
        || key.Equals("priority", StringComparison.OrdinalIgnoreCase)
        || key.Equals("completed", StringComparison.OrdinalIgnoreCase)
        || key.Equals("search", StringComparison.OrdinalIgnoreCase)
        || key.Equals("userId", StringComparison.OrdinalIgnoreCase)
        || key.Equals("skip", StringComparison.OrdinalIgnoreCase)
        || key.Equals("take", StringComparison.OrdinalIgnoreCase);

    internal static string EscapeLike(string value) =>
        value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}
