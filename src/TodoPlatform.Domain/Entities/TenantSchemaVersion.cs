using TodoPlatform.Domain.Tenancy;

namespace TodoPlatform.Domain.Entities;

/// <summary>Logical schema version for a tenant on the shared database (B-12.1).</summary>
public class TenantSchemaVersion
{
    public Guid TenantId { get; private set; }

    public string Track { get; private set; } = MigrationTracks.Stable;

    public long CurrentVersion { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static TenantSchemaVersion Create(Guid tenantId, string track, long currentVersion)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));

        if (!MigrationTracks.IsKnown(track))
            throw new ArgumentException("Track must be 'stable' or 'beta'.", nameof(track));

        return new TenantSchemaVersion
        {
            TenantId = tenantId,
            Track = track.Trim().ToLowerInvariant(),
            CurrentVersion = currentVersion,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>Bump logical schema version after a successful apply (B-12.5).</summary>
    public void ApplyVersion(long version)
    {
        if (version <= CurrentVersion)
            throw new InvalidOperationException(
                $"Cannot apply version {version}: tenant is already at {CurrentVersion}.");

        CurrentVersion = version;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
