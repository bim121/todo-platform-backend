using System.Net.Http.Headers;
using TodoPlatform.Api.Extensions;
using TodoPlatform.Api.Middleware;
using TodoPlatform.Domain.Tenancy;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Api.Tests.Infrastructure;

public static class TestHttpClientExtensions
{
    public static HttpClient CreateAuthenticatedClient(
        this TodoPlatformWebApplicationFactory factory,
        string email = DbSeeder.TestEmail,
        params string[] roles)
    {
        var client = factory.CreateClient();
        ApplyAuth(client, email, TestAuthHandler.DefaultTestSub, roles);
        ApplyDefaultTenant(client);
        return client;
    }

    public static HttpClient CreateAuthenticatedClient(
        this TodoPlatformWebApplicationFactory factory,
        string email,
        string keycloakSub,
        params string[] roles)
    {
        var client = factory.CreateClient();
        ApplyAuth(client, email, keycloakSub, roles);
        ApplyDefaultTenant(client);
        return client;
    }

    public static HttpClient CreateAuthenticatedClientWithoutTenant(
        this TodoPlatformWebApplicationFactory factory,
        string email = DbSeeder.TestEmail,
        params string[] roles)
    {
        var client = factory.CreateClient();
        ApplyAuth(client, email, TestAuthHandler.DefaultTestSub, roles);
        return client;
    }

    private static void ApplyAuth(HttpClient client, string email, string keycloakSub, string[] roles)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestAuthHandler.TestToken);
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserEmailHeader, email);
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserSubHeader, keycloakSub);
        if (roles.Length > 0)
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserRolesHeader, string.Join(',', roles));
    }

    private static void ApplyDefaultTenant(HttpClient client) =>
        client.DefaultRequestHeaders.Add(TenantResolutionMiddleware.HeaderName, WellKnownTenants.DefaultSlug);
}
