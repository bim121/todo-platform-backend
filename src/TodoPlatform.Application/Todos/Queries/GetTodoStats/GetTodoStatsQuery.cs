using MediatR;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Exceptions;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Services;

namespace TodoPlatform.Application.Todos.Queries.GetTodoStats;

public sealed record GetTodoStatsQuery(Guid? UserId = null) : IRequest<TodoStatsDto>;

public sealed class GetTodoStatsQueryHandler(
    ITodoStatsReadStore statsStore,
    ICurrentUserService currentUser)
    : IRequestHandler<GetTodoStatsQuery, TodoStatsDto>
{
    public async Task<TodoStatsDto> Handle(
        GetTodoStatsQuery request,
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

        return await statsStore.GetByUserIdAsync(userId, cancellationToken);
    }
}
