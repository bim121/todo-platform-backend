using TodoPlatform.Domain.Common;
using TodoPlatform.Domain.Enums;
using TodoPlatform.Domain.Events;

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

        var todo = new Todo
        {
            Title = title.Trim(),
            UserId = userId,
            Status = status,
            Priority = priority,
            Completed = false
        };

        todo.RaiseDomainEvent(new TodoCreatedEvent(todo.Id, userId, todo.Title));
        return todo;
    }

    public void Complete()
    {
        if (Completed)
            return;

        Completed = true;
        Status = TodoStatus.Done;
        RaiseDomainEvent(new TodoCompletedEvent(Id, UserId));
    }

    public void MarkDeleted() =>
        RaiseDomainEvent(new TodoDeletedEvent(Id, UserId));

    public void UpdateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        Title = title.Trim();
    }

    public void SetCompleted(bool completed)
    {
        if (completed)
        {
            Complete();
            return;
        }

        Completed = false;
        if (Status == TodoStatus.Done)
            Status = TodoStatus.Todo;
    }

    public void UpdateStatus(TodoStatus status)
    {
        Status = status;
        Completed = status == TodoStatus.Done;
    }
}
