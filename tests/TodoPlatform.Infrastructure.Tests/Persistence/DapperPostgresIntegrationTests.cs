using Dapper;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Domain.Tenancy;
using TodoPlatform.Infrastructure.Migrations;
using TodoPlatform.Infrastructure.Persistence;
using TodoPlatform.Infrastructure.Tests.Support;

namespace TodoPlatform.Infrastructure.Tests.Persistence;

/// <summary>
/// B-10.8 — Dapper read models against real Postgres (Testcontainers).
/// </summary>
public sealed class DapperPostgresIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private string _connectionString = "";

    public async Task InitializeAsync()
    {
        if (!DockerEnvironment.IsAvailable)
            return;

        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("tododb")
            .WithUsername("todo")
            .WithPassword("todo")
            // V007 creates pg_stat_statements — requires preload on first start.
            .WithCommand("-c", "shared_preload_libraries=pg_stat_statements")
            .Build();

        await _postgres.StartAsync();
        _connectionString = _postgres.GetConnectionString();

        var services = new ServiceCollection();
        services.AddFluentMigrator(_connectionString);
        await using var sp = services.BuildServiceProvider();
        sp.GetRequiredService<IMigrationRunner>().MigrateUp();
    }

    public async Task DisposeAsync()
    {
        if (_postgres is not null)
            await _postgres.DisposeAsync();
    }

    [DockerFact]
    public async Task TodoStats_And_SystemStats_ReturnExpectedAggregates()
    {
        Assert.False(string.IsNullOrEmpty(_connectionString));

        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        await using (var conn = new NpgsqlConnection(_connectionString))
        {
            await conn.OpenAsync();
            await SeedUsersAndTodosAsync(conn, userA, userB);
        }

        IReadDbConnection readDb = new DapperReadDbConnection(
            _connectionString,
            new StaticTenantContext(WellKnownTenants.DefaultId));
        var todoStats = new DapperTodoStatsReadStore(readDb);
        var systemStats = new DapperSystemStatsReadStore(readDb);

        var a = await todoStats.GetByUserIdAsync(userA);
        Assert.Equal(3, a.Total);
        Assert.Equal(2, a.Active);
        Assert.Equal(1, a.Completed);

        var empty = await todoStats.GetByUserIdAsync(Guid.NewGuid());
        Assert.Equal(0, empty.Total);

        var system = await systemStats.GetAsync();
        Assert.Equal(2, system.TotalUsers);
        Assert.Equal(4, system.TotalTodos);
        Assert.Equal(2.00m, system.AvgTodosPerUser);
    }

    private static async Task SeedUsersAndTodosAsync(
        NpgsqlConnection conn,
        Guid userA,
        Guid userB)
    {
        await conn.ExecuteAsync(
            """
            INSERT INTO users ("Id", "Email", "PasswordHash", "Name", "KeycloakSub", "TenantId")
            VALUES
              (@A, 'a@example.com', 'x', 'A', NULL, @TenantId),
              (@B, 'b@example.com', 'x', 'B', NULL, @TenantId);
            """,
            new { A = userA, B = userB, TenantId = WellKnownTenants.DefaultId });

        // userA: 2 active + 1 completed; userB: 1 active
        await conn.ExecuteAsync(
            """
            INSERT INTO todos ("Id", "Title", "Completed", "UserId", "Status", "Priority", "TenantId")
            VALUES
              (@Id1, 'A1', false, @A, 'Todo', 'Medium', @TenantId),
              (@Id2, 'A2', false, @A, 'InProgress', 'High', @TenantId),
              (@Id3, 'A3', true,  @A, 'Done', 'Low', @TenantId),
              (@Id4, 'B1', false, @B, 'Todo', 'Medium', @TenantId);
            """,
            new
            {
                A = userA,
                B = userB,
                TenantId = WellKnownTenants.DefaultId,
                Id1 = Guid.NewGuid(),
                Id2 = Guid.NewGuid(),
                Id3 = Guid.NewGuid(),
                Id4 = Guid.NewGuid()
            });
    }
}
