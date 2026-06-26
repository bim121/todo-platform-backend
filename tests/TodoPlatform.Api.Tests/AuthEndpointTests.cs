using System.Net;
using System.Net.Http.Json;
using TodoPlatform.Api.Tests.Infrastructure;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Api.Tests;

public sealed class AuthEndpointTests : IClassFixture<TodoPlatformWebApplicationFactory>, IAsyncLifetime
{
    private readonly TodoPlatformWebApplicationFactory _factory;
    private HttpClient _userClient = null!;

    public AuthEndpointTests(TodoPlatformWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        await _factory.EnsureDatabaseSeededAsync();
        _userClient = _factory.CreateAuthenticatedClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

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
        var adminClient = _factory.CreateAuthenticatedClient(DbSeeder.TestEmail, "admin", "user");
        var response = await adminClient.GetAsync("/api/admin/tenants");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tenants = await response.Content.ReadFromJsonAsync<List<TenantAdminDto>>();
        Assert.NotNull(tenants);
    }
}
