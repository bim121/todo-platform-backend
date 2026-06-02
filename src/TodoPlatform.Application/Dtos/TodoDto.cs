using TodoPlatform.Application.Mapping;
using TodoPlatform.Domain.Entities;

namespace TodoPlatform.Application.Dtos;

public sealed record TodoDto(
    Guid Id,
    string Title,
    bool Completed,
    Guid UserId,
    string Status,
    string Priority)
{
    public static TodoDto FromEntity(Todo todo) =>
        new(
            todo.Id,
            todo.Title,
            todo.Completed,
            todo.UserId,
            TodoContractMapper.ToApiStatus(todo.Status),
            TodoContractMapper.ToApiPriority(todo.Priority));
}
