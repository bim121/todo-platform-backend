namespace TodoPlatform.Domain.Tenancy;

/// <summary>Per-tenant schema tracks (B-12). Blue/green deploy tracks are B-28.</summary>
public static class MigrationTracks
{
    public const string Stable = "stable";
    public const string Beta = "beta";

    public static bool IsKnown(string track) =>
        string.Equals(track, Stable, StringComparison.OrdinalIgnoreCase)
        || string.Equals(track, Beta, StringComparison.OrdinalIgnoreCase);
}
