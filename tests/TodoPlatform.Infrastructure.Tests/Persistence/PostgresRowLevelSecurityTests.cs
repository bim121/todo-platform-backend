using Dapper;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;
using TodoPlatform.Domain.Tenancy;
using TodoPlatform.Infrastructure.Migrations;
using TodoPlatform.Infrastructure.Tests.Support;

namespace TodoPlatform.Infrastructure.Tests.Persistence;

/// <summary>
/// B-11.7 — RLS isolation on a non-superuser role (docker POSTGRES_USER bypasses RLS).
/// </summary>
public sealed class PostgresRowLevelSecurityTests : IAsyncLifetime
{
    private const string AppUser = "todo_app";
    private const string AppPassword = "todo_app";

    private PostgreSqlContainer? _postgres;
    private string _superuserConnectionString = "";
    private string _appConnectionString = "";

    public async Task InitializeAsync()
    {
        if (!DockerEnvironment.IsAvailable)
            return;

        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("tododb")
            .WithUsername("todo")
            .WithPassword("todo")
            .WithCommand("-c", "shared_preload_libraries=pg_stat_statements")
            .Build();

        await _postgres.StartAsync();
        _superuserConnectionString = _postgres.GetConnectionString();

        var services = new ServiceCollection();
        services.AddFluentMigrator(_superuserConnectionString);
        await using var sp = services.BuildServiceProvider();
        sp.GetRequiredService<IMigrationRunner>().MigrateUp();

        await CreateAppRoleAsync();
        _appConnectionString = new NpgsqlConnectionStringBuilder(_superuserConnectionString)
        {
            Username = AppUser,
            Password = AppPassword
        }.ConnectionString;
    }

    public async Task DisposeAsync()
    {
        if (_postgres is not null)
            await _postgres.DisposeAsync();
    }

    [DockerFact]
    public async Task WithoutTenantSetting_AppRoleSeesNoTodos()
    {
        await SeedCrossTenantTodosAsync();

        await using var connection = new NpgsqlConnection(_appConnectionString);
        await connection.OpenAsync();

        var count = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM todos");
        Assert.Equal(0, count);
    }

    [DockerFact]
    public async Task WithTenantSetting_AppRoleSeesOnlyThatTenant()
    {
        var (defaultTodoId, acmeTodoId) = await SeedCrossTenantTodosAsync();

        await using var connection = new NpgsqlConnection(_appConnectionString);
        await connection.OpenAsync();

        await connection.ExecuteAsync(
            "SELECT set_config('app.current_tenant', @tenantId, false)",
            new { tenantId = WellKnownTenants.DefaultId.ToString() });

        var titles = (await connection.QueryAsync<string>("SELECT \"Title\" FROM todos")).ToList();
        Assert.Equal(["default-only"], titles);

        var acmeVisible = await connection.ExecuteScalarAsync<int>(
            """SELECT COUNT(*) FROM todos WHERE "Id" = @Id""",
            new { Id = acmeTodoId });
        Assert.Equal(0, acmeVisible);

        await connection.ExecuteAsync(
            "SELECT set_config('app.current_tenant', @tenantId, false)",
            new { tenantId = WellKnownTenants.AcmeId.ToString() });

        titles = (await connection.QueryAsync<string>("SELECT \"Title\" FROM todos")).ToList();
        Assert.Equal(["acme-only"], titles);

        var defaultVisible = await connection.ExecuteScalarAsync<int>(
            """SELECT COUNT(*) FROM todos WHERE "Id" = @Id""",
            new { Id = defaultTodoId });
        Assert.Equal(0, defaultVisible);
    }

    [DockerFact]
    public async Task Superuser_BypassesRlsWithoutTenantSetting()
    {
        await SeedCrossTenantTodosAsync();

        await using var connection = new NpgsqlConnection(_superuserConnectionString);
        await connection.OpenAsync();

        var count = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM todos");
        Assert.Equal(2, count);
    }

    [DockerFact]
    public async Task BypassGuc_LetsAppRoleReadAllTenants()
    {
        await SeedCrossTenantTodosAsync();

        await using var connection = new NpgsqlConnection(_appConnectionString);
        await connection.OpenAsync();

        await connection.ExecuteAsync("SELECT set_config('app.bypass_rls', 'true', false)");

        var count = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM todos");
        Assert.Equal(2, count);
    }

    private async Task CreateAppRoleAsync()
    {
        await using var connection = new NpgsqlConnection(_superuserConnectionString);
        await connection.OpenAsync();

        await connection.ExecuteAsync(
            $"""
            CREATE ROLE {AppUser} LOGIN PASSWORD '{AppPassword}'
                NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOBYPASSRLS;
            GRANT CONNECT ON DATABASE tododb TO {AppUser};
            GRANT USAGE ON SCHEMA public TO {AppUser};
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO {AppUser};
            GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO {AppUser};
            """);
    }

    private async Task<(Guid DefaultTodoId, Guid AcmeTodoId)> SeedCrossTenantTodosAsync()
    {
        var defaultUser = Guid.NewGuid();
        var acmeUser = Guid.NewGuid();
        var defaultTodo = Guid.NewGuid();
        var acmeTodo = Guid.NewGuid();

        await using var connection = new NpgsqlConnection(_superuserConnectionString);
        await connection.OpenAsync();

        await connection.ExecuteAsync(
            """
            TRUNCATE todos, users RESTART IDENTITY CASCADE;

            INSERT INTO users ("Id", "Email", "PasswordHash", "Name", "KeycloakSub", "TenantId")
            VALUES
              (@DefaultUser, 'rls-a@example.com', 'x', 'A', NULL, @DefaultTenant),
              (@AcmeUser, 'rls-b@example.com', 'x', 'B', NULL, @AcmeTenant);

            INSERT INTO todos ("Id", "Title", "Completed", "UserId", "Status", "Priority", "TenantId")
            VALUES
              (@DefaultTodo, 'default-only', false, @DefaultUser, 'Todo', 'Medium', @DefaultTenant),
              (@AcmeTodo, 'acme-only', false, @AcmeUser, 'Todo', 'Medium', @AcmeTenant);
            """,
            new
            {
                DefaultUser = defaultUser,
                AcmeUser = acmeUser,
                DefaultTodo = defaultTodo,
                AcmeTodo = acmeTodo,
                DefaultTenant = WellKnownTenants.DefaultId,
                AcmeTenant = WellKnownTenants.AcmeId
            });

        return (defaultTodo, acmeTodo);
    }
}
