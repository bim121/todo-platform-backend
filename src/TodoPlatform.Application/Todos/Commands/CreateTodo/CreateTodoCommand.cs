using MediatR;
using TodoPlatform.Application.Common;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Tenancy;
using TodoPlatform.Domain.Entities;

namespace TodoPlatform.Application.Todos.Commands.CreateTodo;

public sealed record CreateTodoCommand(string Title, Guid UserId) : IRequest<TodoDto>, ICommand;

public sealed class CreateTodoHandler(ITodoRepository repository, ITenantContext tenantContext)
    : IRequestHandler<CreateTodoCommand, TodoDto>
{
    public async Task<TodoDto> Handle(CreateTodoCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var tenantId = tenantContext.RequireTenantId();
            var todo = Todo.Create(request.Title, request.UserId, tenantId: tenantId);
            await repository.AddAsync(todo, cancellationToken);
            return TodoDto.FromEntity(todo);
        }
        catch (ArgumentException ex)
        {
            throw Exceptions.ValidationException.ForField("title", ex.Message);
        }
    }
}
