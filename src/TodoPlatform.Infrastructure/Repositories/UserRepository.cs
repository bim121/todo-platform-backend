using Microsoft.EntityFrameworkCore;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Domain.Entities;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Infrastructure.Repositories;

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<User?> GetByKeycloakSubAsync(string keycloakSub, CancellationToken cancellationToken = default) =>
        db.Users.FirstOrDefaultAsync(u => u.KeycloakSub == keycloakSub, cancellationToken);

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        db.Users.AnyAsync(u => u.Email == email, cancellationToken);

    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        db.Users.Update(user);
        await db.SaveChangesAsync(cancellationToken);
    }
}
