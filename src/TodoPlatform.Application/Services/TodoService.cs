using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Interfaces;

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
        var todo = request.ToEntity();
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

        request.ApplyTo(todo);
        await repository.UpdateAsync(todo, cancellationToken);
        return TodoDto.FromEntity(todo);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        repository.DeleteAsync(id, cancellationToken);
}
