using TodoPlatform.Domain.Tenancy;
using TodoPlatform.Infrastructure.Migrations;

namespace TodoPlatform.Infrastructure.Tests.Migrations;

public sealed class MigrationPlanServiceTests
{
    private readonly MigrationPlanService _sut = new();

    [Fact]
    public void Catalog_IncludesUntaggedAndBetaTaggedMigrations()
    {
        Assert.Contains(_sut.Catalog, m => m.Version == 11 && !m.IsBeta);
        Assert.Contains(_sut.Catalog, m => m.Version == 12 && m.IsBeta);
        Assert.Equal("V012-beta-feature", _sut.Find(12)!.SchemaVersionLabel);
        Assert.Equal("V011", _sut.Find(11)!.SchemaVersionLabel);
    }

    [Fact]
    public void LatestStableVersion_ExcludesBeta()
    {
        Assert.Equal(11, _sut.LatestStableVersion);
        Assert.True(_sut.Catalog.Max(m => m.Version) > _sut.LatestStableVersion);
    }

    [Fact]
    public void GetPending_StableAtLatest_IsEmpty()
    {
        var pending = _sut.GetPending(MigrationTracks.Stable, _sut.LatestStableVersion);
        Assert.Empty(pending);
    }

    [Fact]
    public void GetPending_BetaAtLatestStable_IncludesBetaFeature()
    {
        var pending = _sut.GetPending(MigrationTracks.Beta, _sut.LatestStableVersion);
        Assert.Contains(pending, m => m.Version == 12 && m.IsBeta);
    }

    [Fact]
    public void GetPending_StableBehind_DoesNotIncludeBeta()
    {
        var pending = _sut.GetPending(MigrationTracks.Stable, currentVersion: 10);
        Assert.Contains(pending, m => m.Version == 11);
        Assert.DoesNotContain(pending, m => m.IsBeta);
    }
}
