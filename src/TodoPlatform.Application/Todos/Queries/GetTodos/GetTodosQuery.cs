using MediatR;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Exceptions;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Services;
using TodoPlatform.Application.Todos.Specifications;

namespace TodoPlatform.Application.Todos.Queries.GetTodos;

public sealed record GetTodosQuery(
    Guid? UserId = null,
    bool ActiveOnly = false,
    int? Skip = null,
    int? Take = null) : IRequest<IReadOnlyList<TodoDto>>;

public sealed class GetTodosQueryHandler(
    ITodoRepository repository,
    ICurrentUserService currentUser)
    : IRequestHandler<GetTodosQuery, IReadOnlyList<TodoDto>>
{
    public async Task<IReadOnlyList<TodoDto>> Handle(
        GetTodosQuery request,
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

        var specification = TodoListSpecification.Create(
            userId,
            request.ActiveOnly,
            request.Skip,
            request.Take);

        var todos = await repository.ListAsync(specification, cancellationToken);
        return todos.Select(TodoDto.FromEntity).ToList();
    }
}
