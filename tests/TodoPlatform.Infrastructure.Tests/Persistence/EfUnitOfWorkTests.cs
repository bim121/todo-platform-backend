using Microsoft.EntityFrameworkCore;
using Moq;
using TodoPlatform.Application.Common;
using TodoPlatform.Domain.Entities;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Infrastructure.Tests.Persistence;

public sealed class EfUnitOfWorkTests
{
    [Fact]
    public async Task CommitAsync_PersistsEntityAndDispatchesDomainEvents()
    {
        var options = CreateOptions();
        await using var db = new AppDbContext(options);
        var dispatcher = new Mock<IDomainEventDispatcher>();

        var unitOfWork = new EfUnitOfWork(db, dispatcher.Object);
        var todo = Todo.Create("Test", Guid.NewGuid());
        db.Todos.Add(todo);

        await unitOfWork.CommitAsync();

        Assert.Single(await db.Todos.ToListAsync());
        dispatcher.Verify(
            d => d.DispatchEventsAsync(
                It.Is<IEnumerable<Domain.Common.IDomainEvent>>(events => events.Any()),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CommitAsync_WhenSaveChangesFails_DoesNotDispatchEvents()
    {
        var options = CreateOptions();
        await using var db = new ThrowingDbContext(options);
        var dispatcher = new Mock<IDomainEventDispatcher>();

        var unitOfWork = new EfUnitOfWork(db, dispatcher.Object);
        db.Todos.Add(Todo.Create("Test", Guid.NewGuid()));

        await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.CommitAsync());

        dispatcher.Verify(
            d => d.DispatchEventsAsync(It.IsAny<IEnumerable<Domain.Common.IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

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
