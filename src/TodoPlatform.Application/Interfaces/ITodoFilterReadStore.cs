using TodoPlatform.Application.Dtos;

namespace TodoPlatform.Application.Interfaces;

public interface ITodoFilterReadStore
{
    Task<PagedResult<TodoListItemDto>> SearchAsync(
        Guid userId,
        string? status,
        string? priority,
        bool? completed,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}
