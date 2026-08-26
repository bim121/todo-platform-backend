namespace TodoPlatform.Infrastructure.Migrations;

/// <summary>
/// Physical FluentMigrator version numbers for tenant-stream DDL (B-12.12).
/// Logical catalog stays 1–12; tenant <c>VersionInfo</c> uses 1000+ band.
/// </summary>
public static class TenantPhysicalMigrationVersions
{
    public const long Baseline = 1001;

    public const long BetaFeaturePreview = 1012;

    public static long ForLogical(long logicalVersion) =>
        logicalVersion switch
        {
            12 => BetaFeaturePreview,
            <= 11 => Baseline,
            _ => throw new ArgumentOutOfRangeException(nameof(logicalVersion), logicalVersion, "Unknown logical version.")
        };
}
