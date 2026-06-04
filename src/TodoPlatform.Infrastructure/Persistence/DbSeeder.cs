using Microsoft.EntityFrameworkCore;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Domain.Entities;
using TodoPlatform.Domain.Enums;

namespace TodoPlatform.Infrastructure.Persistence;

public sealed class DbSeeder(AppDbContext db, IPasswordHasher passwordHasher)
{
    public const string TestEmail = "test@example.com";
    public const string TestPassword = "password123";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == TestEmail, cancellationToken);

        if (user is null)
        {
            user = User.Register(
                TestEmail,
                passwordHasher.Hash(TestPassword),
                "Test User");
            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);
        }

        if (await db.Todos.AnyAsync(t => t.UserId == user.Id, cancellationToken))
            return;

        db.Todos.AddRange(
            Todo.Create("Learn NgRx Effects", user.Id),
            Todo.Create(
                "Connect Angular to ASP.NET API",
                user.Id,
                TodoStatus.InProgress,
                TodoPriority.High),
            Todo.Create(
                "Review OpenAPI contract",
                user.Id,
                priority: TodoPriority.Low));

        await db.SaveChangesAsync(cancellationToken);
    }
}
