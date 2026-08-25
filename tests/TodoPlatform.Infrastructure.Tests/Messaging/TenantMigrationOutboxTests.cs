using Microsoft.EntityFrameworkCore;
using Moq;
using TodoPlatform.Application.IntegrationEvents;
using TodoPlatform.Domain.Entities;
using TodoPlatform.Domain.Tenancy;
using TodoPlatform.Infrastructure.Messaging;
using TodoPlatform.Infrastructure.Migrations;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Infrastructure.Tests.Messaging;

public sealed class TenantMigrationOutboxTests
{
    [Fact]
    public async Task CommitAsync_StagesTenantMigrationAppliedOutboxMessage()
    {
        var databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        await using var db = new AppDbContext(options);
        db.Tenants.Add(Tenant.Create(WellKnownTenants.AcmeSlug, WellKnownTenants.AcmeName, WellKnownTenants.AcmeId));
        db.TenantSchemaVersions.Add(
            TenantSchemaVersion.Create(WellKnownTenants.AcmeId, MigrationTracks.Beta, 11));
        await db.SaveChangesAsync();

        var dispatcher = new Moq.Mock<TodoPlatform.Application.Common.IDomainEventDispatcher>();
        dispatcher
            .Setup(d => d.DispatchEventsAsync(It.IsAny<IEnumerable<TodoPlatform.Domain.Common.IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mapper = new DomainEventToIntegrationEventMapper();
        var unitOfWork = new EfUnitOfWork(db, dispatcher.Object, new EfOutboxStore(db, mapper));
        var runner = new LogicalTenantMigrationRunner(
            db,
            new EfTenantSchemaVersionStore(db),
            new MigrationPlanService(),
            new TenantMigrationCompatibilityValidator(db));

        await runner.ApplyAsync(WellKnownTenants.AcmeId, 12, "admin@test", cancellationToken: CancellationToken.None);
        await unitOfWork.CommitAsync();

        var outbox = Assert.Single(await db.OutboxMessages.ToListAsync());
        Assert.Equal(TenantMigrationAppliedIntegrationEvent.EventTypeName, outbox.Type);
        Assert.Contains("V012-beta-feature", outbox.Payload, StringComparison.Ordinal);
    }
}
