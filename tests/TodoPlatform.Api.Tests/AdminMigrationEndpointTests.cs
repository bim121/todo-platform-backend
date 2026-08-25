using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoPlatform.Api.Tests.Infrastructure;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Domain.Tenancy;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Api.Tests;

/// <summary>B-12.9 — admin migration plan / apply integration tests.</summary>
[Collection(nameof(TodoPlatformWebApplicationFactory))]
public sealed class AdminMigrationEndpointTests : IClassFixture<TodoPlatformWebApplicationFactory>, IAsyncLifetime
{
    private readonly TodoPlatformWebApplicationFactory _factory;
    private HttpClient _adminClient = null!;

    public AdminMigrationEndpointTests(TodoPlatformWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        await _factory.EnsureDatabaseSeededAsync();
        _adminClient = _factory.CreateAuthenticatedClient(
            "admin@example.com",
            "33333333-3333-3333-3333-333333333333",
            "admin",
            "user");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task MigrationPlan_StableTenant_HasNoBetaPending()
    {
        await ResetSchemaAsync(WellKnownTenants.DefaultId, MigrationTracks.Stable);

        var response = await _adminClient.GetAsync(
            $"/api/admin/tenants/{WellKnownTenants.DefaultId}/migration-plan");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var plan = await response.Content.ReadFromJsonAsync<MigrationPlanDto>();
        Assert.NotNull(plan);
        Assert.Equal("stable", plan.Track);
        Assert.Empty(plan.Pending);
    }

    [Fact]
    public async Task MigrationPlan_BetaTenant_IncludesV012Pending()
    {
        await ResetSchemaAsync(WellKnownTenants.AcmeId, MigrationTracks.Beta);

        var response = await _adminClient.GetAsync(
            $"/api/admin/tenants/{WellKnownTenants.AcmeId}/migration-plan");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var plan = await response.Content.ReadFromJsonAsync<MigrationPlanDto>();
        Assert.NotNull(plan);
        Assert.Equal("beta", plan.Track);
        var pending = Assert.Single(plan.Pending);
        Assert.Equal(12, pending.Version);
        Assert.Contains("beta", pending.Tags, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApplyMigration_BetaTenantWithoutTodos_BumpsVersionAndWritesHistory()
    {
        await ResetSchemaAsync(WellKnownTenants.AcmeId, MigrationTracks.Beta);

        var applyResponse = await _adminClient.PostAsJsonAsync(
            $"/api/admin/tenants/{WellKnownTenants.AcmeId}/migrations/apply",
            new ApplyTenantMigrationRequest());

        Assert.Equal(HttpStatusCode.OK, applyResponse.StatusCode);
        var body = await applyResponse.Content.ReadFromJsonAsync<ApplyTenantMigrationResponse>();
        Assert.NotNull(body);
        Assert.False(body.DryRun);
        Assert.NotNull(body.Tenant);
        Assert.Equal("V012-beta-feature", body.Tenant!.SchemaVersion);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.TenantSchemaVersions.SingleAsync(v => v.TenantId == WellKnownTenants.AcmeId);
        Assert.Equal(12, row.CurrentVersion);
        Assert.Contains(
            await db.MigrationHistory.Where(h => h.TenantId == WellKnownTenants.AcmeId).ToListAsync(),
            h => h.Version == "V012-beta-feature");
    }

    [Fact]
    public async Task ApplyMigration_BetaWithExistingTodos_ReturnsConflict()
    {
        await ResetSchemaAsync(WellKnownTenants.DefaultId, MigrationTracks.Beta);

        var response = await _adminClient.PostAsJsonAsync(
            $"/api/admin/tenants/{WellKnownTenants.DefaultId}/migrations/apply",
            new ApplyTenantMigrationRequest());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ApplyMigration_DryRun_DoesNotPersistChanges()
    {
        await ResetSchemaAsync(WellKnownTenants.AcmeId, MigrationTracks.Beta);

        var response = await _adminClient.PostAsync(
            $"/api/admin/tenants/{WellKnownTenants.AcmeId}/migrations/apply?dryRun=true",
            null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApplyTenantMigrationResponse>();
        Assert.NotNull(body);
        Assert.True(body.DryRun);
        Assert.NotNull(body.Preview);
        Assert.NotNull(body.Preview!.WouldApply);
        Assert.Equal(12, body.Preview.WouldApply!.Version);
        Assert.Null(body.Tenant);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.TenantSchemaVersions.SingleAsync(v => v.TenantId == WellKnownTenants.AcmeId);
        Assert.Equal(11, row.CurrentVersion);
        Assert.Empty(await db.MigrationHistory.Where(h => h.TenantId == WellKnownTenants.AcmeId).ToListAsync());
    }

    [Fact]
    public async Task ApplyMigration_StaleExpectedUpdatedAt_ReturnsConflict()
    {
        await ResetSchemaAsync(WellKnownTenants.AcmeId, MigrationTracks.Beta);

        var stale = DateTimeOffset.Parse("2020-01-01T00:00:00Z");
        var response = await _adminClient.PostAsJsonAsync(
            $"/api/admin/tenants/{WellKnownTenants.AcmeId}/migrations/apply",
            new ApplyTenantMigrationRequest(ExpectedUpdatedAt: stale));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private Task ResetSchemaAsync(Guid tenantId, string track, long version = 11) =>
        AdminTestData.ResetTenantSchemaAsync(_factory.Services, tenantId, track, version);
}
