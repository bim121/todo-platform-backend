using TodoPlatform.Domain.Entities;
using TodoPlatform.Domain.Specifications;

namespace TodoPlatform.Application.Todos.Specifications;

public static class TodoListSpecification
{
    public static Specification<Todo> Create(
        Guid userId,
        bool activeOnly = false,
        int? skip = null,
        int? take = null)
    {
        Specification<Todo> spec = new TodoByUserSpecification(userId);

        if (activeOnly)
            spec = spec & new ActiveTodosSpecification();

        if (skip is > 0 || take is > 0)
            spec = spec & new TodoPagingSpecification(skip ?? 0, take);

        return spec;
    }
}
