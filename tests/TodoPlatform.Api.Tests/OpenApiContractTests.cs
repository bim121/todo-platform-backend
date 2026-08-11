using System.Text.Json;
using TodoPlatform.Api.Tests.Infrastructure;

namespace TodoPlatform.Api.Tests;

public sealed class OpenApiContractTests(OpenApiWebApplicationFactory factory)
    : IClassFixture<OpenApiWebApplicationFactory>
{
    private static readonly string[] ImplementedSwaggerPaths =
    [
        "/api/auth/login",
        "/api/auth/me",
        "/api/auth/register",
        "/api/todos",
        "/api/todos/stats",
        "/api/todos/search",
        "/api/todos/{id}",
        "/api/admin/tenants",
        "/api/Health"
    ];

    private static readonly string[] FutureOnlyContractPaths =
    [
        "/users",
        "/tenants/{id}/config",
        "/search"
    ];

    [Fact]
    public async Task Swagger_IncludesImplementedContractPaths()
    {
        var client = factory.CreateClient();
        var json = await client.GetStringAsync("/swagger/v1/swagger.json");
        using var document = JsonDocument.Parse(json);
        var paths = document.RootElement.GetProperty("paths");

        foreach (var path in ImplementedSwaggerPaths)
        {
            Assert.True(
                paths.TryGetProperty(path, out _),
                $"Expected swagger path '{path}' from B-02 contract.");
        }
    }

    [Fact]
    public async Task Swagger_ExcludesFutureContractPaths()
    {
        var client = factory.CreateClient();
        var json = await client.GetStringAsync("/swagger/v1/swagger.json");
        using var document = JsonDocument.Parse(json);
        var paths = document.RootElement.GetProperty("paths");

        foreach (var contractPath in FutureOnlyContractPaths)
        {
            var swaggerPath = contractPath.StartsWith('/') ? $"/api{contractPath}" : $"/api/{contractPath}";
            Assert.False(
                paths.TryGetProperty(swaggerPath, out _),
                $"Future-only contract path '{contractPath}' should not appear in swagger yet.");
        }
    }

    [Fact]
    public async Task Swagger_TodosListDeclaresProblemDetailsForBadRequest()
    {
        var client = factory.CreateClient();
        var json = await client.GetStringAsync("/swagger/v1/swagger.json");
        using var document = JsonDocument.Parse(json);

        var listTodos = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/todos")
            .GetProperty("get");

        Assert.True(listTodos.GetProperty("responses").TryGetProperty("400", out _));
    }
}
