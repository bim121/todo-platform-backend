using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TodoPlatform.Api.Tests.Infrastructure;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Api.Tests;

public sealed class VersioningEndpointTests : IClassFixture<TodoPlatformWebApplicationFactory>
{
    private readonly TodoPlatformWebApplicationFactory _factory;

    public VersioningEndpointTests(TodoPlatformWebApplicationFactory factory) =>
        _factory = factory;

    [Fact]
    public async Task Health_WithAcceptVersionV1_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Accept-Version", "v1");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_WithoutAcceptVersion_DefaultsToV1()
    {
        var response = await _factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_WithUnsupportedVersion_ReturnsValidationProblemDetails()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Accept-Version", "v99");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Validation Error", json.GetProperty("title").GetString());
        Assert.True(json.GetProperty("errors").TryGetProperty("Accept-Version", out _));
    }

    [Fact]
    public async Task Login_ReturnsGoneWithDeprecationHeaders()
    {
        await _factory.EnsureDatabaseSeededAsync();
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = DbSeeder.TestEmail, password = DbSeeder.TestPassword });

        Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Deprecation", out var deprecation));
        Assert.Contains("true", deprecation);
        Assert.True(response.Headers.TryGetValues("Sunset", out var sunset));
        Assert.Contains("Sat, 01 Jun 2027 00:00:00 GMT", sunset);
    }
}
