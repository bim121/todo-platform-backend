using TodoPlatform.Domain.Entities;
using TodoPlatform.Domain.Specifications;

namespace TodoPlatform.Application.Todos.Specifications;

/// <summary>Stable ORDER BY Id for Skip/Take (B-09.4).</summary>
public sealed class TodoOrderByIdSpecification : Specification<Todo>
{
    public TodoOrderByIdSpecification() => OrderById = true;
}
