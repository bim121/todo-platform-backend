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
        _client = _factory.CreateAuthenticatedClient();
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
    public async Task GetTodos_WithPaging_ReturnsSubset()
    {
        var response = await _client.GetAsync($"/api/todos?userId={_userId}&skip=0&take=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var todos = await response.Content.ReadFromJsonAsync<List<TodoDto>>();
        Assert.NotNull(todos);
        Assert.Single(todos);
    }

    [Fact]
    public async Task GetTodos_InvalidTake_ReturnsValidationProblemDetails()
    {
        var response = await _client.GetAsync($"/api/todos?userId={_userId}&take=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetTodo_NotFound_ReturnsProblemDetails()
    {
        var response = await _client.GetAsync($"/api/todos/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("Not Found", json);
    }

    [Fact]
    public async Task CreateTodo_EmptyTitle_ReturnsValidationProblemDetails()
    {
        var request = new CreateTodoRequest("", _userId);

        var response = await _client.PostAsJsonAsync("/api/todos", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("errors", json);
        Assert.Contains("title", json);
    }

    [Fact]
    public async Task GetTodos_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync($"/api/todos?userId={_userId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetTodos_WithoutUserId_UsesAuthenticatedUser()
    {
        var response = await _client.GetAsync("/api/todos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var todos = await response.Content.ReadFromJsonAsync<List<TodoDto>>();
        Assert.NotNull(todos);
        Assert.True(todos.Count >= 3);
        Assert.All(todos, todo => Assert.Equal(_userId, todo.UserId));
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
