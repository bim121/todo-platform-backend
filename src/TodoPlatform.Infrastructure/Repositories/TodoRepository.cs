using Microsoft.EntityFrameworkCore;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Domain.Entities;
using TodoPlatform.Domain.Enums;
using TodoPlatform.Domain.Specifications;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Infrastructure.Repositories;

public sealed class TodoRepository(AppDbContext db, ISpecificationEvaluator evaluator) : ITodoRepository
{
    public async Task<IReadOnlyList<Todo>> ListAsync(
        Specification<Todo> specification,
        CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(db.Todos.AsQueryable(), specification);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TodoDto>> ListDtosAsync(
        Specification<Todo> specification,
        CancellationToken cancellationToken = default)
    {
        // No Include(User) — TodoDto only needs UserId (B-09.4 / B-09.5).
        var query = ApplySpecification(db.Todos.AsQueryable(), specification);

        return await query
            .Select(t => new TodoDto(
                t.Id,
                t.Title,
                t.Completed,
                t.UserId,
                t.Status == TodoStatus.Todo
                    ? "todo"
                    : t.Status == TodoStatus.InProgress
                        ? "in_progress"
                        : "done",
                t.Priority == TodoPriority.Low
                    ? "low"
                    : t.Priority == TodoPriority.High
                        ? "high"
                        : "medium"))
            .ToListAsync(cancellationToken);
    }

    public Task<Todo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Todos.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<Todo> AddAsync(Todo todo, CancellationToken cancellationToken = default)
    {
        db.Todos.Add(todo);
        return Task.FromResult(todo);
    }

    public Task UpdateAsync(Todo todo, CancellationToken cancellationToken = default)
    {
        db.Todos.Update(todo);
        return Task.CompletedTask;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var todo = await db.Todos.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (todo is null)
            return false;

        todo.MarkDeleted();
        db.Todos.Remove(todo);
        return true;
    }

    private IQueryable<Todo> ApplySpecification(
        IQueryable<Todo> source,
        Specification<Todo> specification)
    {
        var query = source;
        if (specification.AsNoTracking)
            query = query.AsNoTracking();

        return evaluator.GetQuery(query, specification);
    }
}
