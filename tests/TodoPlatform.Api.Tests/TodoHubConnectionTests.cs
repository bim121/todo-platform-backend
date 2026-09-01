using Microsoft.AspNetCore.SignalR.Client;
using TodoPlatform.Api.Extensions;
using TodoPlatform.Domain.Tenancy;
using TodoPlatform.Api.Tests.Infrastructure;

namespace TodoPlatform.Api.Tests;

[Collection(nameof(TodoPlatformWebApplicationFactory))]
public sealed class TodoHubConnectionTests(TodoPlatformWebApplicationFactory factory)
{
    [Fact]
    public async Task Connect_WithBearerAndTenantHeader_Succeeds()
    {
        await factory.EnsureDatabaseSeededAsync();

        await using var connection = new HubConnectionBuilder()
            .WithUrl(BuildHubUri(), options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Headers.Add("Authorization", $"Bearer {TestAuthHandler.TestToken}");
                options.Headers.Add("X-Tenant-Id", WellKnownTenants.DefaultSlug);
            })
            .Build();

        await connection.StartAsync();

        Assert.Equal(HubConnectionState.Connected, connection.State);
    }

    [Fact]
    public async Task Connect_WithAccessTokenQuery_Succeeds()
    {
        await factory.EnsureDatabaseSeededAsync();

        var uri = new Uri(
            BuildHubUri(),
            $"?access_token={TestAuthHandler.TestToken}&tenant={WellKnownTenants.DefaultSlug}");

        await using var connection = new HubConnectionBuilder()
            .WithUrl(uri, options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
            })
            .Build();

        await connection.StartAsync();

        Assert.Equal(HubConnectionState.Connected, connection.State);
    }

    [Fact]
    public async Task Connect_WithoutToken_Fails()
    {
        await factory.EnsureDatabaseSeededAsync();

        await using var connection = new HubConnectionBuilder()
            .WithUrl(BuildHubUri(), options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Headers.Add("X-Tenant-Id", WellKnownTenants.DefaultSlug);
            })
            .Build();

        await Assert.ThrowsAsync<HttpRequestException>(() => connection.StartAsync());
    }

    private Uri BuildHubUri() =>
        new(factory.Server.BaseAddress!, "hubs/todos");
}
