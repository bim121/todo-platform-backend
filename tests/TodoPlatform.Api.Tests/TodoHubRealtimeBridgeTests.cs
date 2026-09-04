using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TodoPlatform.Api.Extensions;
using TodoPlatform.Api.Tests.Infrastructure;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Realtime;
using TodoPlatform.Domain.Tenancy;
using TodoPlatform.Infrastructure.Messaging;

namespace TodoPlatform.Api.Tests;

[Collection(nameof(TodoPlatformWebApplicationFactory))]
public sealed class TodoHubRealtimeBridgeTests(TodoPlatformWebApplicationFactory factory)
{
    [Fact]
    public async Task CreateTodo_PushesTodoCreatedToConnectedClient()
    {
        await factory.EnsureDatabaseSeededAsync();
        var userId = await factory.GetTestUserIdAsync();

        var received = new TaskCompletionSource<TodoRealtimeMessage>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        await using var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress!, "hubs/todos"), options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Headers.Add("Authorization", $"Bearer {TestAuthHandler.TestToken}");
                options.Headers.Add("X-Tenant-Id", WellKnownTenants.DefaultSlug);
            })
            .Build();

        connection.On<TodoRealtimeMessage>("TodoCreated", msg => received.TrySetResult(msg));
        await connection.StartAsync();

        var client = factory.CreateAuthenticatedClient();
        var title = $"signalr-create-{Guid.NewGuid():N}";
        var response = await client.PostAsJsonAsync(
            "/api/todos",
            new CreateTodoRequest(title, userId));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<TodoDto>();
        Assert.NotNull(created);

        await FlushOutboxAsync();

        var message = await received.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(created.Id, message.Id);
        Assert.Equal(title, message.Title);
        Assert.False(message.Completed);
    }

    [Fact]
    public async Task UpdateTodo_PushesTodoUpdatedToConnectedClient()
    {
        await factory.EnsureDatabaseSeededAsync();
        var userId = await factory.GetTestUserIdAsync();
        var client = factory.CreateAuthenticatedClient();

        var createdResponse = await client.PostAsJsonAsync(
            "/api/todos",
            new CreateTodoRequest($"signalr-update-seed-{Guid.NewGuid():N}", userId));
        var created = await createdResponse.Content.ReadFromJsonAsync<TodoDto>();
        Assert.NotNull(created);
        await FlushOutboxAsync();

        var received = new TaskCompletionSource<TodoRealtimeMessage>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        await using var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress!, "hubs/todos"), options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Headers.Add("Authorization", $"Bearer {TestAuthHandler.TestToken}");
                options.Headers.Add("X-Tenant-Id", WellKnownTenants.DefaultSlug);
            })
            .Build();

        connection.On<TodoRealtimeMessage>("TodoUpdated", msg => received.TrySetResult(msg));
        await connection.StartAsync();

        var patch = await client.PatchAsJsonAsync(
            $"/api/todos/{created.Id}",
            new UpdateTodoRequest(Title: "signalr-updated-title", Completed: true));
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);

        await FlushOutboxAsync();

        var message = await received.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(created.Id, message.Id);
        Assert.Equal("signalr-updated-title", message.Title);
        Assert.True(message.Completed);
    }

    [Fact]
    public async Task DeleteTodo_PushesTodoDeletedToConnectedClient()
    {
        await factory.EnsureDatabaseSeededAsync();
        var userId = await factory.GetTestUserIdAsync();
        var client = factory.CreateAuthenticatedClient();

        var createdResponse = await client.PostAsJsonAsync(
            "/api/todos",
            new CreateTodoRequest($"signalr-delete-seed-{Guid.NewGuid():N}", userId));
        var created = await createdResponse.Content.ReadFromJsonAsync<TodoDto>();
        Assert.NotNull(created);
        await FlushOutboxAsync();

        var received = new TaskCompletionSource<TodoRealtimeMessage>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        await using var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress!, "hubs/todos"), options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Headers.Add("Authorization", $"Bearer {TestAuthHandler.TestToken}");
                options.Headers.Add("X-Tenant-Id", WellKnownTenants.DefaultSlug);
            })
            .Build();

        connection.On<TodoRealtimeMessage>("TodoDeleted", msg => received.TrySetResult(msg));
        await connection.StartAsync();

        var delete = await client.DeleteAsync($"/api/todos/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        await FlushOutboxAsync();

        var message = await received.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(created.Id, message.Id);
    }

    private async Task FlushOutboxAsync()
    {
        // Give UoW commit a moment to persist outbox rows, then drain manually
        // (OutboxProcessor poll interval is 5s — too slow for tests).
        await Task.Delay(50);

        using var scope = factory.Services.CreateScope();
        var processor = scope.ServiceProvider.GetServices<IHostedService>()
            .OfType<OutboxProcessor>()
            .Single();

        // MassTransit in-memory delivery is async; retry briefly after publish.
        for (var attempt = 0; attempt < 10; attempt++)
        {
            await processor.PublishPendingAsync();
            await Task.Delay(100);
        }
    }
}
