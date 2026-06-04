using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TodoPlatform.Api.Tests.Infrastructure;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Api.Tests;

public class TodosEndpointTests : IClassFixture<TodoPlatformWebApplicationFactory>, IAsyncLifetime
{
    private readonly TodoPlatformWebApplicationFactory _factory;
    private HttpClient _client = null!;
    private Guid _userId;

    public TodosEndpointTests(TodoPlatformWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.EnsureDatabaseSeededAsync();
        _userId = await _factory.GetTestUserIdAsync();
        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateTodo_Returns201()
    {
        var request = new CreateTodoRequest("Integration test todo", _userId);

        var response = await _client.PostAsJsonAsync("/api/todos", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var todo = await response.Content.ReadFromJsonAsync<TodoDto>();
        Assert.NotNull(todo);
        Assert.Equal(request.Title, todo.Title);
        Assert.Equal(_userId, todo.UserId);
        Assert.False(todo.Completed);
        Assert.Equal("todo", todo.Status);
        Assert.Equal("medium", todo.Priority);
    }

    [Fact]
    public async Task GetTodos_ByUserId_Returns200()
    {
        var response = await _client.GetAsync($"/api/todos?userId={_userId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var todos = await response.Content.ReadFromJsonAsync<List<TodoDto>>();
        Assert.NotNull(todos);
        Assert.True(todos.Count >= 3);
    }

    [Fact]
    public async Task Login_WithSeedUser_Returns200()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(DbSeeder.TestEmail, DbSeeder.TestPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        Assert.True(document.RootElement.TryGetProperty("token", out _));
        Assert.Equal(_userId.ToString(), document.RootElement.GetProperty("user").GetProperty("id").GetString());
    }
}
