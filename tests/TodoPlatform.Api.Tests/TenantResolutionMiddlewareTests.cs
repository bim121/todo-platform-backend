using System.Net;
using TodoPlatform.Api.Extensions;
using TodoPlatform.Api.Middleware;
using TodoPlatform.Api.Tests.Infrastructure;
using TodoPlatform.Domain.Tenancy;

namespace TodoPlatform.Api.Tests;

public sealed class TenantResolutionMiddlewareTests
    : IClassFixture<TodoPlatformWebApplicationFactory>, IAsyncLifetime
{
    private readonly TodoPlatformWebApplicationFactory _factory;

    public TenantResolutionMiddlewareTests(TodoPlatformWebApplicationFactory factory) =>
        _factory = factory;

    public Task InitializeAsync() => _factory.EnsureDatabaseSeededAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Authenticated_WithoutTenant_Returns400()
    {
        var client = _factory.CreateAuthenticatedClientWithoutTenant();
        var response = await client.GetAsync("/api/todos");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Authenticated_UnknownTenant_Returns404()
    {
        var client = _factory.CreateAuthenticatedClientWithoutTenant();
        client.DefaultRequestHeaders.Add(TenantResolutionMiddleware.HeaderName, "missing-tenant");

        var response = await client.GetAsync("/api/todos");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Authenticated_JwtClaim_ResolvesTenant()
    {
        var client = _factory.CreateAuthenticatedClientWithoutTenant();
        client.DefaultRequestHeaders.Add(TestAuthHandler.TenantClaimHeader, WellKnownTenants.DefaultSlug);

        var response = await client.GetAsync("/api/todos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_WithoutTenant_ReturnsOk()
    {
        var response = await _factory.CreateClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
