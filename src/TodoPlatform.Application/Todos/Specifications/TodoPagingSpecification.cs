using TodoPlatform.Domain.Entities;
using TodoPlatform.Domain.Specifications;

namespace TodoPlatform.Application.Todos.Specifications;

public sealed class TodoPagingSpecification : Specification<Todo>
{
    public TodoPagingSpecification(int skip, int? take)
    {
        if (skip > 0)
            Skip = skip;

        if (take is > 0)
            Take = take;
    }
}
