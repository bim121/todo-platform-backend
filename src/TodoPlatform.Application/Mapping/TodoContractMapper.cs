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
}
