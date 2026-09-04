using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TodoPlatform.Application.Caching;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Todos.EventHandlers;
using TodoPlatform.Domain.Events;
using TodoPlatform.Domain.Tenancy;

namespace TodoPlatform.Application.Tests.Todos.EventHandlers;

public sealed class TodoCacheInvalidatorTests
{
    [Fact]
    public async Task Created_RemovesUserListPrefix()
    {
        var cache = new Mock<ICacheService>();
        var handler = new TodoCreatedCacheInvalidator(
            cache.Object,
            NullLogger<TodoCreatedCacheInvalidator>.Instance);
        var userId = Guid.NewGuid();
        var tenantId = WellKnownTenants.DefaultId;
        var evt = new TodoCreatedEvent(Guid.NewGuid(), userId, tenantId, "New");

        await handler.Handle(evt, CancellationToken.None);

        cache.Verify(
            c => c.RemoveByPrefixAsync(CacheKeys.TodosByUserPrefix(tenantId, userId), It.IsAny<CancellationToken>()),
            Times.Once);
        cache.Verify(
            c => c.RemoveAsync(CacheKeys.TodoStatsByUser(tenantId, userId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Completed_RemovesTodoAndList()
    {
        var cache = new Mock<ICacheService>();
        var handler = new TodoCompletedCacheInvalidator(
            cache.Object,
            NullLogger<TodoCompletedCacheInvalidator>.Instance);
        var todoId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = WellKnownTenants.AcmeId;

        await handler.Handle(new TodoCompletedEvent(todoId, userId, tenantId), CancellationToken.None);

        cache.Verify(c => c.RemoveAsync(CacheKeys.TodoById(tenantId, todoId), It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(
            c => c.RemoveByPrefixAsync(CacheKeys.TodosByUserPrefix(tenantId, userId), It.IsAny<CancellationToken>()),
            Times.Once);
        cache.Verify(
            c => c.RemoveAsync(CacheKeys.TodoStatsByUser(tenantId, userId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Deleted_RemovesTodoAndList()
    {
        var cache = new Mock<ICacheService>();
        var handler = new TodoDeletedCacheInvalidator(
            cache.Object,
            NullLogger<TodoDeletedCacheInvalidator>.Instance);
        var todoId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = WellKnownTenants.DefaultId;

        await handler.Handle(new TodoDeletedEvent(todoId, userId, tenantId, "gone", false), CancellationToken.None);

        cache.Verify(c => c.RemoveAsync(CacheKeys.TodoById(tenantId, todoId), It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(
            c => c.RemoveByPrefixAsync(CacheKeys.TodosByUserPrefix(tenantId, userId), It.IsAny<CancellationToken>()),
            Times.Once);
        cache.Verify(
            c => c.RemoveAsync(CacheKeys.TodoStatsByUser(tenantId, userId), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
