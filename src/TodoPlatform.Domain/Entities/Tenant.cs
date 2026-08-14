using TodoPlatform.Domain.Common;
using TodoPlatform.Domain.Enums;

namespace TodoPlatform.Domain.Entities;

public class Tenant : Entity
{
    public string Slug { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public TenantStatus Status { get; private set; } = TenantStatus.Active;
    public DateTimeOffset CreatedAt { get; private set; }

    private Tenant()
    {
    }

    public bool IsActive => Status == TenantStatus.Active;

    public static Tenant Create(string slug, string name, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug is required.", nameof(slug));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        var normalized = slug.Trim().ToLowerInvariant();
        if (normalized.Length > 64)
            throw new ArgumentException("Slug must be at most 64 characters.", nameof(slug));

        return new Tenant
        {
            Id = id ?? Guid.NewGuid(),
            Slug = normalized,
            Name = name.Trim(),
            Status = TenantStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Deactivate() => Status = TenantStatus.Inactive;
}
