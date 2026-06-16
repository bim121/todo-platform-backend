using TodoPlatform.Domain.Entities;
using TodoPlatform.Domain.Specifications;

namespace TodoPlatform.Application.Todos.Specifications;

public sealed class TodoByUserSpecification : Specification<Todo>
{
    public TodoByUserSpecification(Guid userId)
    {
        Criteria = todo => todo.UserId == userId;
        ApplyOrderBy(todo => todo.Title);
    }
}
