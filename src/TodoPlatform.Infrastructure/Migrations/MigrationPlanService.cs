using System.Reflection;
using FluentMigrator;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Migrations;
using TodoPlatform.Domain.Tenancy;

namespace TodoPlatform.Infrastructure.Migrations;

/// <summary>
/// Builds the migration catalog from FluentMigrator attributes and computes
/// pending versions for stable vs beta tracks (B-12.2).
/// </summary>
public sealed class MigrationPlanService : IMigrationPlanService
{
    public MigrationPlanService()
    {
        Catalog = typeof(V001_CreateUsersAndTodosTables).Assembly
            .GetTypes()
            .Select(type => (type, migration: type.GetCustomAttribute<MigrationAttribute>()))
            .Where(x => x.migration is not null)
            .Where(x => x.migration!.Version < 1000)
            .Where(x => !IsPlatformOnly(x.type))
            .Select(x => ToInfo(x.type, x.migration!))
            .OrderBy(m => m.Version)
            .ToArray();

        LatestStableVersion = Catalog
            .Where(m => !m.IsBeta)
            .Select(m => m.Version)
            .DefaultIfEmpty(0)
            .Max();
    }

    public IReadOnlyList<MigrationInfo> Catalog { get; }

    public long LatestStableVersion { get; }

    public MigrationInfo? Find(long version) =>
        Catalog.FirstOrDefault(m => m.Version == version);

    public IReadOnlyList<MigrationInfo> GetPending(string track, long currentVersion)
    {
        var includeBeta = string.Equals(track, MigrationTracks.Beta, StringComparison.OrdinalIgnoreCase);
        return Catalog
            .Where(m => m.Version > currentVersion)
            .Where(m => includeBeta || !m.IsBeta)
            .ToArray();
    }

    private static MigrationInfo ToInfo(Type type, MigrationAttribute migration)
    {
        var tags = type.GetCustomAttributes<TagsAttribute>()
            .SelectMany(a => a.TagNames)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var name = string.IsNullOrWhiteSpace(migration.Description)
            ? type.Name
            : migration.Description;

        return new MigrationInfo(migration.Version, name, name, tags);
    }

    private static bool IsPlatformOnly(Type type) =>
        type.GetCustomAttributes<TagsAttribute>()
            .SelectMany(a => a.TagNames)
            .Any(t => string.Equals(t, "platform", StringComparison.OrdinalIgnoreCase));
}
