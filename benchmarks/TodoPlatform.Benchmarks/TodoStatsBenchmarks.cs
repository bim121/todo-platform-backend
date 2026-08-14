using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Dapper;
using FluentMigrator.Runner;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Tenancy;
using TodoPlatform.Domain.Tenancy;
using TodoPlatform.Infrastructure.Migrations;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Benchmarks;

/// <summary>
/// B-10.8 — optional micro-benchmark: EF aggregate vs Dapper view for per-user todo stats.
/// Run: <c>dotnet run -c Release --project benchmarks/TodoPlatform.Benchmarks</c> (Docker required).
/// </summary>
public static class Program
{
    public static void Main(string[] args) =>
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}

[MemoryDiagnoser]
public class TodoStatsBenchmarks
{
    private PostgreSqlContainer _postgres = null!;
    private string _connectionString = "";
    private Guid _userId;
    private ITodoStatsReadStore _dapper = null!;
    private AppDbContext _db = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("tododb")
            .WithUsername("todo")
            .WithPassword("todo")
            .WithCommand("-c", "shared_preload_libraries=pg_stat_statements")
            .Build();

        await _postgres.StartAsync();
        _connectionString = _postgres.GetConnectionString();

        var services = new ServiceCollection();
        services.AddFluentMigrator(_connectionString);
        await using (var sp = services.BuildServiceProvider())
        {
            sp.GetRequiredService<IMigrationRunner>().MigrateUp();
        }

        _userId = Guid.NewGuid();
        await using (var conn = new NpgsqlConnection(_connectionString))
        {
            await conn.OpenAsync();
            await conn.ExecuteAsync(
                """
                INSERT INTO users ("Id", "Email", "PasswordHash", "Name", "TenantId")
                VALUES (@Id, 'bench@example.com', 'x', 'Bench', @TenantId);
                """,
                new { Id = _userId, TenantId = WellKnownTenants.DefaultId });

            for (var i = 0; i < 200; i++)
            {
                await conn.ExecuteAsync(
                    """
                    INSERT INTO todos ("Id", "Title", "Completed", "UserId", "Status", "Priority", "TenantId")
                    VALUES (@Id, @Title, @Completed, @UserId, @Status, 'Medium', @TenantId);
                    """,
                    new
                    {
                        Id = Guid.NewGuid(),
                        Title = $"Todo {i}",
                        Completed = i % 3 == 0,
                        UserId = _userId,
                        Status = i % 3 == 0 ? "Done" : "Todo",
                        TenantId = WellKnownTenants.DefaultId
                    });
            }
        }

        var tenantContext = new TenantContext();
        tenantContext.Set(WellKnownTenants.DefaultId, WellKnownTenants.DefaultSlug);
        _dapper = new DapperTodoStatsReadStore(new DapperReadDbConnection(_connectionString, tenantContext));
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_connectionString)
            .Options);
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _db.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Benchmark(Baseline = true)]
    public async Task<int> Ef_GroupByStats()
    {
        var completed = await _db.Todos.AsNoTracking()
            .Where(t => t.UserId == _userId)
            .Select(t => t.Completed)
            .ToListAsync();

        return completed.Count;
    }

    [Benchmark]
    public async Task<int> Dapper_ViewStats()
    {
        var stats = await _dapper.GetByUserIdAsync(_userId);
        return stats.Total;
    }
}
