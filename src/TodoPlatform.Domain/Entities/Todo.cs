using TodoPlatform.Domain.Common;
using TodoPlatform.Domain.Enums;

namespace TodoPlatform.Domain.Entities;

public class Todo : Entity
{
    public string Title { get; private set; } = string.Empty;
    public bool Completed { get; private set; }
    public Guid UserId { get; private set; }
    public TodoStatus Status { get; private set; } = TodoStatus.Todo;
    public TodoPriority Priority { get; private set; } = TodoPriority.Medium;

    private Todo()
    {
    }

    public static Todo Create(
        string title,
        Guid userId,
        TodoStatus status = TodoStatus.Todo,
        TodoPriority priority = TodoPriority.Medium)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        if (userId == Guid.Empty)
            throw new ArgumentException("User id is required.", nameof(userId));

        return new Todo
        {
            Title = title.Trim(),
            UserId = userId,
            Status = status,
            Priority = priority,
            Completed = false
        };
    }

    public void Complete()
    {
        Completed = true;
        Status = TodoStatus.Done;
    }

    public void UpdateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        Title = title.Trim();
    }
}
