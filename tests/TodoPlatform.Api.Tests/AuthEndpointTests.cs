using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoPlatform.Api.Extensions;
using TodoPlatform.Api.Tests.Infrastructure;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Domain.Tenancy;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Api.Tests;

[Collection(nameof(TodoPlatformWebApplicationFactory))]
public sealed class AuthEndpointTests : IClassFixture<TodoPlatformWebApplicationFactory>, IAsyncLifetime
{
    private readonly TodoPlatformWebApplicationFactory _factory;
    private HttpClient _userClient = null!;
    private Guid _seedUserId;

    public AuthEndpointTests(TodoPlatformWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        await _factory.EnsureDatabaseSeededAsync();
        await AdminTestData.ResetTenantSchemaAsync(_factory.Services, WellKnownTenants.DefaultId, MigrationTracks.Stable);
        await AdminTestData.ResetTenantSchemaAsync(_factory.Services, WellKnownTenants.AcmeId, MigrationTracks.Stable);
        _seedUserId = await _factory.GetTestUserIdAsync();
        _userClient = _factory.CreateAuthenticatedClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetTodos_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/todos");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetTodos_WithUserToken_ReturnsOk()
    {
        var response = await _userClient.GetAsync("/api/todos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var todos = await response.Content.ReadFromJsonAsync<List<TodoDto>>();
        Assert.NotNull(todos);
        Assert.NotEmpty(todos);
    }

    [Fact]
    public async Task GetTenants_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/admin/tenants");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetTenants_WithUserRole_ReturnsForbidden()
    {
        var response = await _userClient.GetAsync("/api/admin/tenants");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetTenants_WithAdminRole_ReturnsOk()
    {
        var adminClient = _factory.CreateAuthenticatedClient(
            "admin@example.com",
            "33333333-3333-3333-3333-333333333333",
            "admin",
            "user");
        var response = await adminClient.GetAsync("/api/admin/tenants");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tenants = await response.Content.ReadFromJsonAsync<PagedResult<TenantAdminDto>>();
        Assert.NotNull(tenants);
        Assert.True(tenants.TotalCount >= 2);
        Assert.Contains(tenants.Items, t => t.Id == WellKnownTenants.DefaultId.ToString()
            && t.DeploymentTrack == "stable"
            && t.SchemaVersion.StartsWith("V", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetMigrationPlan_WithAdminRole_ReturnsPlan()
    {
        await AdminTestData.ResetTenantSchemaAsync(
            _factory.Services,
            WellKnownTenants.DefaultId,
            MigrationTracks.Stable);

        var adminClient = _factory.CreateAuthenticatedClient(
            "admin@example.com",
            "33333333-3333-3333-3333-333333333333",
            "admin",
            "user");
        var response = await adminClient.GetAsync(
            $"/api/admin/tenants/{WellKnownTenants.DefaultId}/migration-plan");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var plan = await response.Content.ReadFromJsonAsync<MigrationPlanDto>();
        Assert.NotNull(plan);
        Assert.Equal("V011", plan.CurrentVersion);
        Assert.Equal("stable", plan.Track);
        Assert.Empty(plan.Pending);
    }

    [Fact]
    public async Task ApplyMigration_StableAtLatest_ReturnsConflict()
    {
        await AdminTestData.ResetTenantSchemaAsync(
            _factory.Services,
            WellKnownTenants.DefaultId,
            MigrationTracks.Stable);

        var adminClient = _factory.CreateAuthenticatedClient(
            "admin@example.com",
            "33333333-3333-3333-3333-333333333333",
            "admin",
            "user");
        var response = await adminClient.PostAsJsonAsync(
            $"/api/admin/tenants/{WellKnownTenants.DefaultId}/migrations/apply",
            new ApplyTenantMigrationRequest());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetTenantById_WithAdminRole_ReturnsSchemaVersion()
    {
        var adminClient = _factory.CreateAuthenticatedClient(
            "admin@example.com",
            "33333333-3333-3333-3333-333333333333",
            "admin",
            "user");
        var response = await adminClient.GetAsync($"/api/admin/tenants/{WellKnownTenants.DefaultId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tenant = await response.Content.ReadFromJsonAsync<TenantAdminDto>();
        Assert.NotNull(tenant);
        Assert.Equal("Default", tenant.Name);
        Assert.Equal("stable", tenant.DeploymentTrack);
        Assert.Equal("V011", tenant.SchemaVersion);
    }

    [Fact]
    public async Task GetTenantById_Unknown_ReturnsNotFound()
    {
        var adminClient = _factory.CreateAuthenticatedClient(
            "admin@example.com",
            "33333333-3333-3333-3333-333333333333",
            "admin",
            "user");
        var response = await adminClient.GetAsync($"/api/admin/tenants/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsGoneWithDeprecationHeaders()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(DbSeeder.TestEmail, DbSeeder.TestPassword));

        Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Deprecation", out var deprecation));
        Assert.Contains("true", deprecation);
        Assert.True(response.Headers.TryGetValues("Sunset", out var sunset));
        Assert.Contains("Sat, 01 Jun 2027 00:00:00 GMT", sunset);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithUserToken_ReturnsProfileLinkedToSeedUser()
    {
        var response = await _userClient.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var me = await response.Content.ReadFromJsonAsync<MeDto>();
        Assert.NotNull(me);
        Assert.Equal(_seedUserId, me.Id);
        Assert.Equal(DbSeeder.TestEmail, me.Email);
        Assert.Equal(TestAuthHandler.DefaultTestSub, me.KeycloakSub);
        Assert.Contains("user", me.Roles);
    }

    [Fact]
    public async Task Sync_CreatesUserOnFirstAuthenticatedRequest()
    {
        const string email = "new-user@example.com";
        const string sub = "22222222-2222-2222-2222-222222222222";
        var client = _factory.CreateAuthenticatedClient(email, sub, "user");

        var response = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == email);

        Assert.Equal(sub, user.KeycloakSub);
        Assert.Equal("Test User", user.Name);
    }
}
