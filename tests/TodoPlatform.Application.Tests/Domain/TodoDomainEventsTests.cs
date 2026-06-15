using TodoPlatform.Domain.Entities;
using TodoPlatform.Domain.Events;

namespace TodoPlatform.Application.Tests.Domain;

public sealed class TodoDomainEventsTests
{
    [Fact]
    public void Create_RaisesTodoCreatedEvent()
    {
        var userId = Guid.NewGuid();
        var todo = Todo.Create("Learn domain events", userId);

        var domainEvent = Assert.Single(todo.DomainEvents);
        var created = Assert.IsType<TodoCreatedEvent>(domainEvent);
        Assert.Equal(todo.Id, created.TodoId);
        Assert.Equal(userId, created.UserId);
        Assert.Equal("Learn domain events", created.Title);
        Assert.True(created.OccurredOn <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Complete_RaisesTodoCompletedEvent()
    {
        var todo = Todo.Create("Task", Guid.NewGuid());
        todo.ClearDomainEvents();

        todo.Complete();

        var domainEvent = Assert.Single(todo.DomainEvents);
        var completed = Assert.IsType<TodoCompletedEvent>(domainEvent);
        Assert.Equal(todo.Id, completed.TodoId);
        Assert.Equal(todo.UserId, completed.UserId);
    }

    [Fact]
    public void Complete_WhenAlreadyCompleted_DoesNotRaiseDuplicateEvent()
    {
        var todo = Todo.Create("Task", Guid.NewGuid());
        todo.Complete();
        todo.ClearDomainEvents();

        todo.Complete();

        Assert.Empty(todo.DomainEvents);
    }

    [Fact]
    public void MarkDeleted_RaisesTodoDeletedEvent()
    {
        var todo = Todo.Create("Task", Guid.NewGuid());

        todo.MarkDeleted();

        Assert.Contains(todo.DomainEvents, e => e is TodoDeletedEvent deleted
            && deleted.TodoId == todo.Id
            && deleted.UserId == todo.UserId);
    }

    [Fact]
    public void SetCompletedTrue_DelegatesToComplete()
    {
        var todo = Todo.Create("Task", Guid.NewGuid());
        todo.ClearDomainEvents();

        todo.SetCompleted(true);

        Assert.True(todo.Completed);
        Assert.Single(todo.DomainEvents, e => e is TodoCompletedEvent);
    }
}
