using MediatR;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Exceptions;
using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Application.Todos.Queries.GetTodoById;

public sealed record GetTodoByIdQuery(Guid Id) : IRequest<TodoDto>;

public sealed class GetTodoByIdQueryHandler(ITodoRepository repository)
    : IRequestHandler<GetTodoByIdQuery, TodoDto>
{
    public async Task<TodoDto> Handle(GetTodoByIdQuery request, CancellationToken cancellationToken)
    {
        var todo = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (todo is null)
            throw new NotFoundException($"Todo '{request.Id}' was not found.");

        return TodoDto.FromEntity(todo);
    }
}
