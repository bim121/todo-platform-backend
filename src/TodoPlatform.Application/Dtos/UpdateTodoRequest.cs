using TodoPlatform.Application.Mapping;
using TodoPlatform.Domain.Entities;

namespace TodoPlatform.Application.Dtos;

public sealed record UpdateTodoRequest(
    string? Title = null,
    bool? Completed = null,
    string? Status = null)
{
    public void ApplyTo(Todo todo)
    {
        if (Title is not null)
            todo.UpdateTitle(Title);

        if (Status is not null)
            todo.UpdateStatus(TodoContractMapper.ParseStatus(Status));

        if (Completed.HasValue)
            todo.SetCompleted(Completed.Value);
    }
}
