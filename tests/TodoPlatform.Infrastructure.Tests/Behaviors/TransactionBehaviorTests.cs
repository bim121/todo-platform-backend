using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using TodoPlatform.Application.Common;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Todos.Commands.CreateTodo;
using TodoPlatform.Domain.Entities;
using TodoPlatform.Infrastructure.Behaviors;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Infrastructure.Tests.Behaviors;

public sealed class TransactionBehaviorTests
{
    [Fact]
    public async Task Handle_ICommand_CommitsUnitOfWorkAfterHandler()
    {
        var options = CreateOptions();
        await using var db = new AppDbContext(options);
        var unitOfWork = new Mock<IUnitOfWork>();
        var behavior = new TransactionBehavior<CreateTodoCommand, object>(db, unitOfWork.Object);

        await behavior.Handle(
            new CreateTodoCommand("Title", Guid.NewGuid()),
            _ => Task.FromResult<object>(new object()),
            CancellationToken.None);

        unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenHandlerThrows_RollsBackWithoutCommit()
    {
        var options = CreateOptions();
        await using var db = new AppDbContext(options);
        var unitOfWork = new Mock<IUnitOfWork>();
        var behavior = new TransactionBehavior<CreateTodoCommand, object>(db, unitOfWork.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(
                new CreateTodoCommand("Title", Guid.NewGuid()),
                _ => throw new InvalidOperationException("Handler failed."),
                CancellationToken.None));

        unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonCommand_SkipsUnitOfWork()
    {
        var options = CreateOptions();
        await using var db = new AppDbContext(options);
        var unitOfWork = new Mock<IUnitOfWork>();
        var behavior = new TransactionBehavior<DummyQuery, string>(db, unitOfWork.Object);

        var result = await behavior.Handle(
            new DummyQuery(),
            _ => Task.FromResult("ok"),
            CancellationToken.None);

        Assert.Equal("ok", result);
        unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static DbContextOptions<AppDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    private sealed record DummyQuery : IRequest<string>;
}
