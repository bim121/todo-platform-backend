using Moq;
using TodoPlatform.Application.Admin.Commands.ApplyTenantMigration;
using TodoPlatform.Application.Admin.Queries.GetMigrationPlan;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Exceptions;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Migrations;
using TodoPlatform.Application.Services;
using TodoPlatform.Domain.Tenancy;

namespace TodoPlatform.Application.Tests.Admin;

public sealed class GetMigrationPlanQueryHandlerTests
{
    [Fact]
    public async Task Handle_StableAtLatest_ReturnsEmptyPending()
    {
        var tenantId = WellKnownTenants.DefaultId;
        var tenants = new Mock<ITenantAdminReadStore>();
        tenants.Setup(s => s.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantAdminDto(
                tenantId.ToString(), "Default", "V011", "stable", "1.0.0", "active"));

        var versions = new Mock<ITenantSchemaVersionStore>();
        versions.Setup(s => s.GetAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantSchemaVersionState(tenantId, MigrationTracks.Stable, 11));

        var plans = new Mock<IMigrationPlanService>();
        plans.Setup(p => p.Find(11)).Returns(new MigrationInfo(11, "V011", "V011", []));
        plans.Setup(p => p.GetPending(MigrationTracks.Stable, 11)).Returns([]);

        var handler = new GetMigrationPlanQueryHandler(tenants.Object, versions.Object, plans.Object);
        var result = await handler.Handle(new GetMigrationPlanQuery(tenantId), CancellationToken.None);

        Assert.Equal("V011", result.CurrentVersion);
        Assert.Equal("stable", result.Track);
        Assert.Empty(result.Pending);
    }

    [Fact]
    public async Task Handle_BetaTrack_IncludesBetaPending()
    {
        var tenantId = WellKnownTenants.AcmeId;
        var tenants = new Mock<ITenantAdminReadStore>();
        tenants.Setup(s => s.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantAdminDto(
                tenantId.ToString(), "Acme", "V011", "beta", "1.0.0", "active"));

        var versions = new Mock<ITenantSchemaVersionStore>();
        versions.Setup(s => s.GetAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantSchemaVersionState(tenantId, MigrationTracks.Beta, 11));

        var plans = new Mock<IMigrationPlanService>();
        plans.Setup(p => p.Find(11)).Returns(new MigrationInfo(11, "V011", "V011", []));
        plans.Setup(p => p.GetPending(MigrationTracks.Beta, 11))
            .Returns([new MigrationInfo(12, "V012_BetaFeaturePreview", "V012_BetaFeaturePreview", ["beta"])]);

        var handler = new GetMigrationPlanQueryHandler(tenants.Object, versions.Object, plans.Object);
        var result = await handler.Handle(new GetMigrationPlanQuery(tenantId), CancellationToken.None);

        var pending = Assert.Single(result.Pending);
        Assert.Equal(12, pending.Version);
        Assert.Contains("beta", pending.Tags);
    }

    [Fact]
    public async Task Handle_UnknownTenant_ThrowsNotFound()
    {
        var tenants = new Mock<ITenantAdminReadStore>();
        tenants.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantAdminDto?)null);

        var handler = new GetMigrationPlanQueryHandler(
            tenants.Object,
            new Mock<ITenantSchemaVersionStore>().Object,
            new Mock<IMigrationPlanService>().Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetMigrationPlanQuery(Guid.NewGuid()), CancellationToken.None));
    }
}

public sealed class ApplyTenantMigrationHandlerTests
{
    [Fact]
    public async Task Handle_AppliesAndReturnsUpdatedDto()
    {
        var tenantId = WellKnownTenants.DefaultId;
        var tenants = new Mock<ITenantAdminReadStore>();
        tenants.Setup(s => s.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantAdminDto(
                tenantId.ToString(), "Default", "V011", "stable", "1.0.0", "active"));

        var runner = new Mock<ITenantMigrationRunner>();
        runner.Setup(r => r.ApplyAsync(tenantId, 12, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantMigrationApplyResult(12, "V012-beta-feature", "beta"));

        var user = new Mock<ICurrentUserService>();
        user.SetupGet(u => u.Email).Returns("admin@example.com");

        var handler = new ApplyTenantMigrationHandler(tenants.Object, runner.Object, user.Object);
        var result = await handler.Handle(
            new ApplyTenantMigrationCommand(tenantId, 12),
            CancellationToken.None);

        Assert.Equal("V012-beta-feature", result.SchemaVersion);
        Assert.Equal("beta", result.DeploymentTrack);
        runner.Verify(r => r.ApplyAsync(tenantId, 12, "admin@example.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MissingTenant_ThrowsNotFound()
    {
        var tenants = new Mock<ITenantAdminReadStore>();
        tenants.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantAdminDto?)null);

        var handler = new ApplyTenantMigrationHandler(
            tenants.Object,
            new Mock<ITenantMigrationRunner>().Object,
            new Mock<ICurrentUserService>().Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ApplyTenantMigrationCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
