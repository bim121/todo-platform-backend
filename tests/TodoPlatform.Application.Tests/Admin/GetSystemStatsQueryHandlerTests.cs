using Moq;
using TodoPlatform.Application.Admin.Queries.GetSystemStats;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Application.Tests.Admin;

public sealed class GetSystemStatsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsStoreResult()
    {
        var expected = new SystemStatsDto(10, 25, 2.5m);
        var store = new Mock<ISystemStatsReadStore>();
        store.Setup(s => s.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var handler = new GetSystemStatsQueryHandler(store.Object);
        var result = await handler.Handle(new GetSystemStatsQuery(), CancellationToken.None);

        Assert.Equal(expected, result);
    }
}
