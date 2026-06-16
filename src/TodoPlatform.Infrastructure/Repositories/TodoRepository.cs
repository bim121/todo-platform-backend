using Microsoft.EntityFrameworkCore;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Domain.Entities;
using TodoPlatform.Domain.Specifications;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Infrastructure.Repositories;

public sealed class TodoRepository(AppDbContext db, ISpecificationEvaluator evaluator) : ITodoRepository
{
    public async Task<IReadOnlyList<Todo>> ListAsync(
        Specification<Todo> specification,
        CancellationToken cancellationToken = default)
    {
        var query = db.Todos.AsQueryable();

        if (specification.AsNoTracking)
            query = query.AsNoTracking();

        query = evaluator.GetQuery(query, specification);
        return await query.ToListAsync(cancellationToken);
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
}
