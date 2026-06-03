using TodoPlatform.Domain.Entities;

namespace TodoPlatform.Application.Dtos;

public sealed record CreateTodoRequest(string Title, Guid UserId)
{
    public Todo ToEntity() => Todo.Create(Title, UserId);
}
