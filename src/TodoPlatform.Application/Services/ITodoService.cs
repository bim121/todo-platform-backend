using TodoPlatform.Application.Dtos;

namespace TodoPlatform.Application.Services;

public interface ITodoService
{
    Task<IReadOnlyList<TodoDto>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<TodoDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TodoDto> CreateAsync(CreateTodoRequest request, CancellationToken cancellationToken = default);

    Task<TodoDto?> UpdateAsync(Guid id, UpdateTodoRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
