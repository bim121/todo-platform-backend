using MediatR;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Todos.Specifications;

namespace TodoPlatform.Application.Todos.Queries.GetTodos;

public sealed record GetTodosQuery(
    Guid UserId,
    bool ActiveOnly = false,
    int? Skip = null,
    int? Take = null) : IRequest<IReadOnlyList<TodoDto>>;

public sealed class GetTodosQueryHandler(ITodoRepository repository)
    : IRequestHandler<GetTodosQuery, IReadOnlyList<TodoDto>>
{
    public async Task<IReadOnlyList<TodoDto>> Handle(
        GetTodosQuery request,
        CancellationToken cancellationToken)
    {
        var specification = TodoListSpecification.Create(
            request.UserId,
            request.ActiveOnly,
            request.Skip,
            request.Take);

        var todos = await repository.ListAsync(specification, cancellationToken);
        return todos.Select(TodoDto.FromEntity).ToList();
    }
}
