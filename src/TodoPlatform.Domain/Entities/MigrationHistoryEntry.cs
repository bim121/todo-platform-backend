using TodoPlatform.Domain.Common;

namespace TodoPlatform.Domain.Entities;

/// <summary>Audit row for a logical per-tenant schema apply (B-12.1 / B-12.8).</summary>
public class MigrationHistoryEntry : Entity
{
    public Guid TenantId { get; private set; }

    public string Version { get; private set; } = string.Empty;

    public DateTimeOffset AppliedAt { get; private set; }

    public string AppliedBy { get; private set; } = string.Empty;

    public static MigrationHistoryEntry Record(Guid tenantId, string version, string appliedBy)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Version is required.", nameof(version));

        if (string.IsNullOrWhiteSpace(appliedBy))
            throw new ArgumentException("Applied-by is required.", nameof(appliedBy));

        return new MigrationHistoryEntry
        {
            TenantId = tenantId,
            Version = version.Trim(),
            AppliedAt = DateTimeOffset.UtcNow,
            AppliedBy = appliedBy.Trim()
        };
    }
}
