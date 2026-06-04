using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Api.Tests.Infrastructure;

public sealed class TodoPlatformWebApplicationFactory : WebApplicationFactory<Program>
{
    private bool _seeded;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }

    public async Task EnsureDatabaseSeededAsync()
    {
        if (_seeded)
            return;

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
        await seeder.SeedAsync();

        _seeded = true;
    }

    public async Task<Guid> GetTestUserIdAsync()
    {
        await EnsureDatabaseSeededAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == DbSeeder.TestEmail);
        return user.Id;
    }
}
