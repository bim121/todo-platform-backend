using System.Net;
using System.Net.Http.Json;
using TodoPlatform.Api.Middleware;
using TodoPlatform.Api.Tests.Infrastructure;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Domain.Tenancy;

namespace TodoPlatform.Api.Tests;

public sealed class CrossTenantIsolationTests
    : IClassFixture<TodoPlatformWebApplicationFactory>, IAsyncLifetime
{
    private readonly TodoPlatformWebApplicationFactory _factory;
    private Guid _userId;

    public CrossTenantIsolationTests(TodoPlatformWebApplicationFactory factory) =>
        _factory = factory;

    public async Task InitializeAsync()
    {
        await _factory.EnsureDatabaseSeededAsync();
        _userId = await _factory.GetTestUserIdAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateInTenantA_IsInvisibleToTenantB()
    {
        var title = $"cross-tenant-{Guid.NewGuid():N}";
        var tenantA = _factory.CreateAuthenticatedClient();

        var created = await tenantA.PostAsJsonAsync(
            "/api/todos",
            new CreateTodoRequest(title, _userId));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var todo = await created.Content.ReadFromJsonAsync<TodoDto>();
        Assert.NotNull(todo);

        var tenantB = _factory.CreateAuthenticatedClientWithoutTenant();
        tenantB.DefaultRequestHeaders.Add(TenantResolutionMiddleware.HeaderName, WellKnownTenants.AcmeSlug);

        var list = await tenantB.GetAsync("/api/todos");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var todos = await list.Content.ReadFromJsonAsync<List<TodoDto>>();
        Assert.NotNull(todos);
        Assert.DoesNotContain(todos, t => t.Id == todo.Id || t.Title == title);

        var byId = await tenantB.GetAsync($"/api/todos/{todo.Id}");
        Assert.Equal(HttpStatusCode.NotFound, byId.StatusCode);

        var fromA = await tenantA.GetAsync($"/api/todos/{todo.Id}");
        Assert.Equal(HttpStatusCode.OK, fromA.StatusCode);
    }
}
