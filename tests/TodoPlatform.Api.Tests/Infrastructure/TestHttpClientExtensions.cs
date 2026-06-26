using System.Net.Http.Headers;
using TodoPlatform.Api.Extensions;
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
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestAuthHandler.TestToken);
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserEmailHeader, email);
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserSubHeader, TestAuthHandler.DefaultTestSub);
        if (roles.Length > 0)
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserRolesHeader, string.Join(',', roles));
        return client;
    }

    public static HttpClient CreateAuthenticatedClient(
        this TodoPlatformWebApplicationFactory factory,
        string email,
        string keycloakSub,
        params string[] roles)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestAuthHandler.TestToken);
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserEmailHeader, email);
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserSubHeader, keycloakSub);
        if (roles.Length > 0)
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserRolesHeader, string.Join(',', roles));
        return client;
    }
}
