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
        "/api/admin/stats",
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

    [Fact]
    public async Task Swagger_AuthenticatedTodosDeclareTenantIdHeader()
    {
        var client = factory.CreateClient();
        var json = await client.GetStringAsync("/swagger/v1/swagger.json");
        using var document = JsonDocument.Parse(json);

        var listTodos = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/todos")
            .GetProperty("get");

        Assert.True(
            HasHeaderParameter(listTodos, "X-Tenant-Id"),
            "Authenticated todos operations must document X-Tenant-Id (B-11.6).");
    }

    [Fact]
    public async Task Swagger_AnonymousHealthOmitsTenantIdHeader()
    {
        var client = factory.CreateClient();
        var json = await client.GetStringAsync("/swagger/v1/swagger.json");
        using var document = JsonDocument.Parse(json);

        var health = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/Health")
            .GetProperty("get");

        Assert.False(HasHeaderParameter(health, "X-Tenant-Id"));
    }

    private static bool HasHeaderParameter(JsonElement operation, string name)
    {
        if (!operation.TryGetProperty("parameters", out var parameters)
            || parameters.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var parameter in parameters.EnumerateArray())
        {
            var parameterName = parameter.TryGetProperty("name", out var n) ? n.GetString() : null;
            var location = parameter.TryGetProperty("in", out var loc) ? loc.GetString() : null;
            if (string.Equals(parameterName, name, StringComparison.OrdinalIgnoreCase)
                && string.Equals(location, "header", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
