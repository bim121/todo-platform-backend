using Microsoft.EntityFrameworkCore;
using Moq;
using TodoPlatform.Application.Common;
using TodoPlatform.Application.IntegrationEvents;
using TodoPlatform.Domain.Entities;
using TodoPlatform.Domain.Events;
using TodoPlatform.Infrastructure.Messaging;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Infrastructure.Tests.Persistence;

public sealed class EfUnitOfWorkTests
{
    [Fact]
    public async Task CommitAsync_PersistsEntityDispatchesEventsAndWritesOutboxRow()
    {
        var options = CreateOptions();
        await using var db = new AppDbContext(options);
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var unitOfWork = CreateUnitOfWork(db, dispatcher.Object);

        var todo = Todo.Create("Test", Guid.NewGuid());
        db.Todos.Add(todo);

        await unitOfWork.CommitAsync();

        Assert.Single(await db.Todos.ToListAsync());
        var outbox = await db.OutboxMessages.ToListAsync();
        Assert.Single(outbox);
        Assert.Equal(TodoCreatedIntegrationEvent.EventTypeName, outbox[0].Type);
        Assert.Contains("\"type\":\"TodoCreatedIntegrationEvent\"", outbox[0].Payload, StringComparison.Ordinal);
        Assert.Contains("\"version\":1", outbox[0].Payload, StringComparison.Ordinal);
        Assert.Contains("\"data\":", outbox[0].Payload, StringComparison.Ordinal);
        Assert.Null(outbox[0].ProcessedAt);
        Assert.Empty(todo.DomainEvents);

        dispatcher.Verify(
            d => d.DispatchEventsAsync(
                It.Is<IEnumerable<Domain.Common.IDomainEvent>>(events =>
                    events.OfType<TodoCreatedEvent>().Any()),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CommitAsync_WhenSaveChangesFails_DoesNotDispatchOrPersistOutbox()
    {
        var options = CreateOptions();
        await using var db = new ThrowingDbContext(options);
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var unitOfWork = CreateUnitOfWork(db, dispatcher.Object);
        db.Todos.Add(Todo.Create("Test", Guid.NewGuid()));

        await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.CommitAsync());

        Assert.Empty(await db.OutboxMessages.ToListAsync());
        dispatcher.Verify(
            d => d.DispatchEventsAsync(It.IsAny<IEnumerable<Domain.Common.IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RollbackAsync_ClearsTrackedEntitiesEventsAndOutboxStaging()
    {
        var options = CreateOptions();
        await using var db = new AppDbContext(options);
        var unitOfWork = CreateUnitOfWork(db, Mock.Of<IDomainEventDispatcher>());

        var todo = Todo.Create("Rollback", Guid.NewGuid());
        db.Todos.Add(todo);
        Assert.Single(todo.DomainEvents);

        await unitOfWork.RollbackAsync();

        Assert.Empty(db.ChangeTracker.Entries());
        Assert.Empty(todo.DomainEvents);
        Assert.Empty(await db.Todos.ToListAsync());
        Assert.Empty(await db.OutboxMessages.ToListAsync());
    }

    [Fact]
    public void Repository_ReturnsSameInstancePerEntityType()
    {
        var options = CreateOptions();
        using var db = new AppDbContext(options);
        var unitOfWork = CreateUnitOfWork(db, Mock.Of<IDomainEventDispatcher>());

        var first = unitOfWork.Repository<Todo>();
        var second = unitOfWork.Repository<Todo>();

        Assert.Same(first, second);
    }

    [Fact]
    public void Add_StagesEntityInChangeTracker()
    {
        var options = CreateOptions();
        using var db = new AppDbContext(options);
        var unitOfWork = CreateUnitOfWork(db, Mock.Of<IDomainEventDispatcher>());
        var todo = Todo.Create("Staged", Guid.NewGuid());

        unitOfWork.Add(todo);

        Assert.Single(db.ChangeTracker.Entries<Todo>());
    }

    private static EfUnitOfWork CreateUnitOfWork(AppDbContext db, IDomainEventDispatcher dispatcher) =>
        new(db, dispatcher, new EfOutboxStore(db, new DomainEventToIntegrationEventMapper()));

    private static DbContextOptions<AppDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    private sealed class ThrowingDbContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
    {
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Save failed.");
    }
}
