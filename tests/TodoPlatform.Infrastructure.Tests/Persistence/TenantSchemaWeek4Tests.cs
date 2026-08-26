using Dapper;
using FluentMigrator.Runner;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;
using TodoPlatform.Domain.Entities;
using TodoPlatform.Domain.Tenancy;
using TodoPlatform.Infrastructure.Migrations;
using TodoPlatform.Infrastructure.Persistence;
using TodoPlatform.Infrastructure.Tenancy;
using TodoPlatform.Infrastructure.Tests.Support;

namespace TodoPlatform.Infrastructure.Tests.Persistence;

/// <summary>B-12.14 — per-tenant schema DDL, search_path pooling, V013 cutover.</summary>
public sealed class TenantSchemaWeek4Tests : IAsyncLifetime
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
            .Build();

        await _postgres.StartAsync();
        _connectionString = _postgres.GetConnectionString();
    }

    public async Task DisposeAsync()
    {
        if (_postgres is not null)
            await _postgres.DisposeAsync();
    }

    [DockerFact]
    public async Task ApplyBeta_OnlyAcmeTenant_HasBetaPreviewFlagsTable()
    {
        await MigrateThroughV012Async();
        await InsertPublicTenantDataAsync();
        MigrateUpPlatform(13);

        await using var db = CreateDbContext();
        await db.Database.ExecuteSqlRawAsync(
            """
            UPDATE tenant_schema_versions
            SET "Track" = 'beta'
            WHERE "TenantId" = {0}
            """,
            WellKnownTenants.AcmeId);

        var runner = CreatePhysicalRunner(db);
        await runner.ApplyAsync(WellKnownTenants.AcmeId, 12, "admin@test");
        await db.SaveChangesAsync();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var acmeHasBeta = await TableExistsAsync(connection, "tenant_acme_corp", "beta_preview_flags");
        var defaultHasBeta = await TableExistsAsync(connection, "tenant_default", "beta_preview_flags");

        Assert.True(acmeHasBeta);
        Assert.False(defaultHasBeta);
    }

    [DockerFact]
    public async Task V013Cutover_TenantSchemaTodoCount_MatchesPublicBeforeCutover()
    {
        await MigrateThroughV012Async();
        var expectedDefault = await InsertPublicTenantDataAsync();

        MigrateUpPlatform(13);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var tenantCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)::int
            FROM tenant_default.todos
            WHERE "TenantId" = @TenantId
            """,
            new { TenantId = WellKnownTenants.DefaultId });

        Assert.Equal(expectedDefault, tenantCount);
    }

    [DockerFact]
    public async Task SearchPath_ResetOnClose_DoesNotLeakBetweenPooledConnections()
    {
        await MigrateThroughV012Async();
        MigrateUpPlatform(13);

        var builder = new NpgsqlConnectionStringBuilder(_connectionString)
        {
            MaxPoolSize = 2,
            MinPoolSize = 1
        };
        var poolCs = builder.ConnectionString;

        await using (var acme = new NpgsqlConnection(poolCs))
        {
            await acme.OpenAsync();
            TenantSession.Apply(acme, WellKnownTenants.AcmeId, TenantSchemaNaming.FromSlug(WellKnownTenants.AcmeSlug));
            var path = await acme.ExecuteScalarAsync<string>("SHOW search_path");
            Assert.Contains("tenant_acme_corp", path, StringComparison.Ordinal);
        }

        await using var next = new NpgsqlConnection(poolCs);
        await next.OpenAsync();
        var freshPath = await next.ExecuteScalarAsync<string>("SHOW search_path");
        Assert.DoesNotContain("tenant_acme_corp", freshPath, StringComparison.Ordinal);

        TenantSession.Apply(next, WellKnownTenants.DefaultId, TenantSchemaNaming.FromSlug(WellKnownTenants.DefaultSlug));
        var defaultPath = await next.ExecuteScalarAsync<string>("SHOW search_path");
        Assert.Contains("tenant_default", defaultPath, StringComparison.Ordinal);
    }

    [DockerFact]
    public async Task CreateTenantProvisioner_ProducesSchemaAtLatestStable()
    {
        await MigrateThroughV012Async();
        MigrateUpPlatform(13);

        var tenantId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        await using var db = CreateDbContext();
        db.Tenants.Add(Tenant.Create("gamma", "Gamma Inc", tenantId));
        db.TenantSchemaVersions.Add(
            TenantSchemaVersion.Create(tenantId, MigrationTracks.Stable, 11));
        await db.SaveChangesAsync();

        var provisioner = new TenantSchemaProvisioner(db, new TenantFluentMigrator(BuildConfiguration()));
        await provisioner.ProvisionAsync(tenantId);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var exists = await TableExistsAsync(connection, "tenant_gamma", "todos");
        Assert.True(exists);

        var version = await connection.ExecuteScalarAsync<long>(
            """
            SELECT "Version"
            FROM tenant_gamma."VersionInfo"
            ORDER BY "Version" DESC
            LIMIT 1
            """);

        Assert.Equal(TenantPhysicalMigrationVersions.Baseline, version);
    }

    private async Task MigrateThroughV012Async()
    {
        MigrateUpPlatform(12);
        await Task.CompletedTask;
    }

    private void MigrateUpPlatform(long version)
    {
        var services = new ServiceCollection();
        services.AddFluentMigrator(_connectionString);
        using var sp = services.BuildServiceProvider();
        sp.GetRequiredService<IMigrationRunner>().MigrateUp(version);
    }

    private async Task<int> InsertPublicTenantDataAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var userId = Guid.NewGuid();
        var todoId = Guid.NewGuid();

        await connection.ExecuteAsync(
            """
            INSERT INTO users ("Id", "Email", "PasswordHash", "Name", "KeycloakSub", "TenantId")
            VALUES (@UserId, 'cutover@test.com', 'hash', 'Cutover', NULL, @TenantId)
            ON CONFLICT DO NOTHING;

            INSERT INTO todos ("Id", "Title", "Completed", "UserId", "Status", "Priority", "TenantId")
            VALUES (@TodoId, 'cutover-todo', false, @UserId, 'Todo', 'Medium', @TenantId);
            """,
            new
            {
                UserId = userId,
                TodoId = todoId,
                TenantId = WellKnownTenants.DefaultId
            });

        return await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)::int FROM public.todos WHERE "TenantId" = @TenantId
            """,
            new { TenantId = WellKnownTenants.DefaultId });
    }

    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_connectionString)
            .Options;
        return new AppDbContext(options);
    }

    private PhysicalTenantMigrationRunner CreatePhysicalRunner(AppDbContext db) =>
        new(
            db,
            new EfTenantSchemaVersionStore(db),
            new MigrationPlanService(),
            new TenantMigrationCompatibilityValidator(db),
            new TenantFluentMigrator(BuildConfiguration()));

    private IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _connectionString
            })
            .Build();

    private static async Task<bool> TableExistsAsync(
        NpgsqlConnection connection,
        string schema,
        string table)
    {
        return await connection.ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE table_schema = @Schema
                  AND table_name = @Table
            )
            """,
            new { Schema = schema, Table = table });
    }
}
