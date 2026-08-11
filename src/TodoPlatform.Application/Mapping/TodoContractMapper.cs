using TodoPlatform.Domain.Enums;

namespace TodoPlatform.Application.Mapping;

public static class TodoContractMapper
{
    public static string ToApiStatus(TodoStatus status) =>
        status switch
        {
            TodoStatus.Todo => "todo",
            TodoStatus.InProgress => "in_progress",
            TodoStatus.Done => "done",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

    public static string ToApiPriority(TodoPriority priority) =>
        priority switch
        {
            TodoPriority.Low => "low",
            TodoPriority.Medium => "medium",
            TodoPriority.High => "high",
            _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null)
        };

    public static TodoStatus ParseStatus(string value) =>
        value switch
        {
            "todo" => TodoStatus.Todo,
            "in_progress" => TodoStatus.InProgress,
            "done" => TodoStatus.Done,
            _ => throw new ArgumentException($"Unknown todo status: {value}.", nameof(value))
        };

    public static TodoPriority ParsePriority(string value) =>
        value switch
        {
            "low" => TodoPriority.Low,
            "medium" => TodoPriority.Medium,
            "high" => TodoPriority.High,
            _ => throw new ArgumentException($"Unknown todo priority: {value}.", nameof(value))
        };

    public static bool TryParseStatus(string? value, out TodoStatus status)
    {
        status = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            status = ParseStatus(value.Trim());
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static bool TryParsePriority(string? value, out TodoPriority priority)
    {
        priority = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            priority = ParsePriority(value.Trim());
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>EF stores enums via <c>HasConversion&lt;string&gt;</c> as enum names (e.g. InProgress).</summary>
    public static string ToDbStatusName(TodoStatus status) => status.ToString();

    public static string ToDbPriorityName(TodoPriority priority) => priority.ToString();
}
