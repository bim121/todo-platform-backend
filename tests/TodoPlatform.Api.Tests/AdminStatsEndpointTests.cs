using System.Net;
using System.Net.Http.Json;
using TodoPlatform.Api.Tests.Infrastructure;
using TodoPlatform.Application.Dtos;

namespace TodoPlatform.Api.Tests;

public sealed class AdminStatsEndpointTests : IClassFixture<TodoPlatformWebApplicationFactory>, IAsyncLifetime
{
    private readonly TodoPlatformWebApplicationFactory _factory;

    public AdminStatsEndpointTests(TodoPlatformWebApplicationFactory factory) => _factory = factory;

    public Task InitializeAsync() => _factory.EnsureDatabaseSeededAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetSystemStats_WithAdmin_ReturnsAggregates()
    {
        var admin = _factory.CreateAuthenticatedClient(
            "admin@example.com",
            "33333333-3333-3333-3333-333333333333",
            "admin",
            "user");

        var response = await admin.GetAsync("/api/admin/stats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stats = await response.Content.ReadFromJsonAsync<SystemStatsDto>();
        Assert.NotNull(stats);
        Assert.True(stats.TotalUsers >= 1);
        Assert.True(stats.TotalTodos >= 3);
        Assert.True(stats.AvgTodosPerUser > 0);
    }

    [Fact]
    public async Task GetSystemStats_WithUserRole_ReturnsForbidden()
    {
        var user = _factory.CreateAuthenticatedClient();
        var response = await user.GetAsync("/api/admin/stats");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
