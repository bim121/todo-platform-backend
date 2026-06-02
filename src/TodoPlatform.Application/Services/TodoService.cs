using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Mapping;
using TodoPlatform.Domain.Entities;

namespace TodoPlatform.Application.Services;

public sealed class TodoService(ITodoRepository repository) : ITodoService
{
    public async Task<IReadOnlyList<TodoDto>> ListByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var todos = await repository.GetByUserIdAsync(userId, cancellationToken);
        return todos.Select(TodoDto.FromEntity).ToList();
    }

    public async Task<TodoDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var todo = await repository.GetByIdAsync(id, cancellationToken);
        return todo is null ? null : TodoDto.FromEntity(todo);
    }

    public async Task<TodoDto> CreateAsync(
        CreateTodoRequest request,
        CancellationToken cancellationToken = default)
    {
        var todo = Todo.Create(request.Title, request.UserId);
        await repository.AddAsync(todo, cancellationToken);
        return TodoDto.FromEntity(todo);
    }

    public async Task<TodoDto?> UpdateAsync(
        Guid id,
        UpdateTodoRequest request,
        CancellationToken cancellationToken = default)
    {
        var todo = await repository.GetByIdAsync(id, cancellationToken);
        if (todo is null)
            return null;

        if (request.Title is not null)
            todo.UpdateTitle(request.Title);

        if (request.Status is not null)
            todo.UpdateStatus(TodoContractMapper.ParseStatus(request.Status));

        if (request.Completed.HasValue)
            todo.SetCompleted(request.Completed.Value);

        await repository.UpdateAsync(todo, cancellationToken);
        return TodoDto.FromEntity(todo);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        repository.DeleteAsync(id, cancellationToken);
}
