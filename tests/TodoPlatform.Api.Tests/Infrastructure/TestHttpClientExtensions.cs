using System.Net.Http.Headers;
using TodoPlatform.Infrastructure.Persistence;

using TodoPlatform.Api.Extensions;

namespace TodoPlatform.Api.Tests.Infrastructure;

public static class TestHttpClientExtensions
{
    public const string TestBearerToken = "test";

    public static HttpClient CreateAuthenticatedClient(
        this TodoPlatformWebApplicationFactory factory,
        string email = DbSeeder.TestEmail,
        params string[] roles)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestBearerToken);
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserEmailHeader, email);
        if (roles.Length > 0)
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserRolesHeader, string.Join(',', roles));
        return client;
    }
}
