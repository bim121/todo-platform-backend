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
    public async Task GetTodoStats_ReturnsAggregatesForUser()
    {
        var response = await _client.GetAsync($"/api/todos/stats?userId={_userId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var stats = await response.Content.ReadFromJsonAsync<TodoStatsDto>();
        Assert.NotNull(stats);
        Assert.Equal(_userId, stats.UserId);
        Assert.True(stats.Total >= 3);
        Assert.Equal(stats.Active + stats.Completed, stats.Total);
    }

    [Fact]
    public async Task SearchTodos_FiltersByPriority_ReturnsPagedResult()
    {
        var response = await _client.GetAsync(
            $"/api/todos/search?userId={_userId}&priority=high&skip=0&take=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.Content.ReadFromJsonAsync<PagedResult<TodoListItemDto>>();
        Assert.NotNull(page);
        Assert.True(page.TotalCount >= 1);
        Assert.All(page.Items, item => Assert.Equal("high", item.Priority));
        Assert.Equal(0, page.Skip);
        Assert.Equal(10, page.Take);
    }

    [Fact]
    public async Task SearchTodos_ContradictoryFilters_ReturnsValidationProblemDetails()
    {
        var response = await _client.GetAsync(
            $"/api/todos/search?userId={_userId}&status=done&completed=false");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetTodos_100Parallel_AllSucceed()
    {
        var tasks = Enumerable.Range(0, 100)
            .Select(_ => _client.GetAsync($"/api/todos?userId={_userId}"))
            .ToArray();

        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
    }
}
