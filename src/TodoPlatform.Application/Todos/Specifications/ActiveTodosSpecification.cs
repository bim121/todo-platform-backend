using TodoPlatform.Domain.Entities;
using TodoPlatform.Domain.Specifications;

namespace TodoPlatform.Application.Todos.Specifications;

public sealed class ActiveTodosSpecification : Specification<Todo>
{
    public ActiveTodosSpecification()
    {
        Criteria = todo => !todo.Completed;
    }
}
