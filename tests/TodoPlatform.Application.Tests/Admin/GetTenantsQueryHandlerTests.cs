using Moq;
using TodoPlatform.Application.Admin.Queries.GetTenantById;
using TodoPlatform.Application.Admin.Queries.GetTenants;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Exceptions;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Domain.Tenancy;

namespace TodoPlatform.Application.Tests.Admin;

public sealed class GetTenantsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsStorePage()
    {
        var expected = new PagedResult<TenantAdminDto>(
            [
                new(
                    WellKnownTenants.DefaultId.ToString(),
                    "Default",
                    "V011",
                    "stable",
                    "1.0.0",
                    "active")
            ],
            TotalCount: 1,
            Skip: 0,
            Take: 20);

        var store = new Mock<ITenantAdminReadStore>();
        store.Setup(s => s.ListAsync(
                It.Is<TenantAdminListFilter>(f => f.Skip == 0 && f.Take == 20 && f.Track == "stable"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetTenantsQueryHandler(store.Object);
        var result = await handler.Handle(
            new GetTenantsQuery(Track: "stable"),
            CancellationToken.None);

        Assert.Equal(expected, result);
    }
}

public sealed class GetTenantByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsTenant()
    {
        var dto = new TenantAdminDto(
            WellKnownTenants.AcmeId.ToString(),
            "Acme Corp",
            "V011",
            "stable",
            "1.0.0",
            "active");
        var store = new Mock<ITenantAdminReadStore>();
        store.Setup(s => s.GetByIdAsync(WellKnownTenants.AcmeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var handler = new GetTenantByIdQueryHandler(store.Object);
        var result = await handler.Handle(new GetTenantByIdQuery(WellKnownTenants.AcmeId), CancellationToken.None);

        Assert.Equal(dto, result);
    }

    [Fact]
    public async Task Handle_MissingTenant_ThrowsNotFound()
    {
        var store = new Mock<ITenantAdminReadStore>();
        store.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantAdminDto?)null);

        var handler = new GetTenantByIdQueryHandler(store.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetTenantByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
