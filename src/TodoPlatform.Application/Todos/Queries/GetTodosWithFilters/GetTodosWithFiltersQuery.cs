using MediatR;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Exceptions;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Services;

namespace TodoPlatform.Application.Todos.Queries.GetTodosWithFilters;

public sealed record GetTodosWithFiltersQuery(
    Guid? UserId = null,
    string? Status = null,
    string? Priority = null,
    bool? Completed = null,
    string? Search = null,
    int Skip = 0,
    int Take = 20) : IRequest<PagedResult<TodoListItemDto>>;

public sealed class GetTodosWithFiltersQueryHandler(
    ITodoFilterReadStore filterStore,
    ICurrentUserService currentUser)
    : IRequestHandler<GetTodosWithFiltersQuery, PagedResult<TodoListItemDto>>
{
    public async Task<PagedResult<TodoListItemDto>> Handle(
        GetTodosWithFiltersQuery request,
        CancellationToken cancellationToken)
    {
        var userId = request.UserId ?? currentUser.UserId;
        if (userId == Guid.Empty)
        {
            throw new ValidationException(
                "User id is required.",
                new Dictionary<string, string[]>
                {
                    ["userId"] = ["Query parameter 'userId' is required when the caller is not authenticated."]
                });
        }

        return await filterStore.SearchAsync(
            userId,
            request.Status,
            request.Priority,
            request.Completed,
            request.Search,
            request.Skip,
            request.Take,
            cancellationToken);
    }
}
