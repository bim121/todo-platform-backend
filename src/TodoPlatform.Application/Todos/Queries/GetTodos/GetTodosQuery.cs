using MediatR;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Exceptions;
using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Application.Todos.Queries.GetTodos;

public sealed record GetTodosQuery(Guid UserId) : IRequest<IReadOnlyList<TodoDto>>;

public sealed class GetTodosQueryHandler(ITodoRepository repository)
    : IRequestHandler<GetTodosQuery, IReadOnlyList<TodoDto>>
{
    public async Task<IReadOnlyList<TodoDto>> Handle(
        GetTodosQuery request,
        CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
            throw ValidationException.ForField("userId", "Query parameter 'userId' is required.");

        var todos = await repository.GetByUserIdAsync(request.UserId, cancellationToken);
        return todos.Select(TodoDto.FromEntity).ToList();
    }
}
