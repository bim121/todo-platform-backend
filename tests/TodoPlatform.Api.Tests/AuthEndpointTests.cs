using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoPlatform.Api.Extensions;
using TodoPlatform.Api.Tests.Infrastructure;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Api.Tests;

public sealed class AuthEndpointTests : IClassFixture<TodoPlatformWebApplicationFactory>, IAsyncLifetime
{
    private readonly TodoPlatformWebApplicationFactory _factory;
    private HttpClient _userClient = null!;
    private Guid _seedUserId;

    public AuthEndpointTests(TodoPlatformWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        await _factory.EnsureDatabaseSeededAsync();
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

        var tenants = await response.Content.ReadFromJsonAsync<List<TenantAdminDto>>();
        Assert.NotNull(tenants);
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
