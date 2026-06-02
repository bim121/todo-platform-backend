using Microsoft.EntityFrameworkCore;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Domain.Entities;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Infrastructure.Repositories;

public sealed class TodoRepository(AppDbContext db) : ITodoRepository
{
    public async Task<IReadOnlyList<Todo>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await db.Todos
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.Title)
            .ToListAsync(cancellationToken);

    public Task<Todo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Todos.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<Todo> AddAsync(Todo todo, CancellationToken cancellationToken = default)
    {
        db.Todos.Add(todo);
        await db.SaveChangesAsync(cancellationToken);
        return todo;
    }

    public async Task UpdateAsync(Todo todo, CancellationToken cancellationToken = default)
    {
        db.Todos.Update(todo);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var todo = await db.Todos.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (todo is null)
            return false;

        db.Todos.Remove(todo);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
