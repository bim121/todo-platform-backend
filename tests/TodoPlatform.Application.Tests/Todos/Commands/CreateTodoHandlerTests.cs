using Moq;
using TodoPlatform.Application.Exceptions;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Tenancy;
using TodoPlatform.Application.Todos.Commands.CreateTodo;
using TodoPlatform.Domain.Entities;
using TodoPlatform.Domain.Tenancy;

namespace TodoPlatform.Application.Tests.Todos.Commands;

public sealed class CreateTodoHandlerTests
{
    [Fact]
    public async Task Handle_CreatesTodoAndReturnsDto()
    {
        var userId = Guid.NewGuid();
        Todo? saved = null;

        var repository = new Mock<ITodoRepository>();
        repository
            .Setup(r => r.AddAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()))
            .Callback<Todo, CancellationToken>((todo, _) => saved = todo)
            .ReturnsAsync((Todo todo, CancellationToken _) => todo);

        var handler = new CreateTodoHandler(repository.Object, ResolvedTenant().Object);
        var result = await handler.Handle(new CreateTodoCommand("Learn MediatR", userId), CancellationToken.None);

        Assert.Equal("Learn MediatR", result.Title);
        Assert.Equal(userId, result.UserId);
        Assert.False(result.Completed);
        Assert.NotNull(saved);
        Assert.Equal("Learn MediatR", saved!.Title);
        Assert.Equal(WellKnownTenants.DefaultId, saved.TenantId);
    }

    [Fact]
    public async Task Handle_AssignsTenantIdFromContext_NotDefaultFallback()
    {
        Todo? saved = null;
        var repository = new Mock<ITodoRepository>();
        repository
            .Setup(r => r.AddAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()))
            .Callback<Todo, CancellationToken>((todo, _) => saved = todo)
            .ReturnsAsync((Todo todo, CancellationToken _) => todo);

        var handler = new CreateTodoHandler(
            repository.Object,
            ResolvedTenant(WellKnownTenants.AcmeId, WellKnownTenants.AcmeSlug).Object);

        await handler.Handle(new CreateTodoCommand("Tenant scoped", Guid.NewGuid()), CancellationToken.None);

        Assert.NotNull(saved);
        Assert.Equal(WellKnownTenants.AcmeId, saved!.TenantId);
    }

    [Fact]
    public async Task Handle_EmptyTitle_ThrowsValidationException()
    {
        var repository = new Mock<ITodoRepository>();
        var handler = new CreateTodoHandler(repository.Object, ResolvedTenant().Object);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new CreateTodoCommand("   ", Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UnresolvedTenant_ThrowsValidationException()
    {
        var repository = new Mock<ITodoRepository>();
        var tenant = new Mock<ITenantContext>();
        tenant.SetupGet(t => t.IsResolved).Returns(false);
        tenant.SetupGet(t => t.TenantId).Returns(Guid.Empty);

        var handler = new CreateTodoHandler(repository.Object, tenant.Object);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new CreateTodoCommand("Learn MediatR", Guid.NewGuid()), CancellationToken.None));

        Assert.True(ex.Errors.ContainsKey("X-Tenant-Id"));
        repository.Verify(
            r => r.AddAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static Mock<ITenantContext> ResolvedTenant(
        Guid? tenantId = null,
        string slug = WellKnownTenants.DefaultSlug)
    {
        var id = tenantId ?? WellKnownTenants.DefaultId;
        var tenant = new Mock<ITenantContext>();
        tenant.SetupGet(t => t.TenantId).Returns(id);
        tenant.SetupGet(t => t.Slug).Returns(slug);
        tenant.SetupGet(t => t.IsResolved).Returns(true);
        return tenant;
    }
}
