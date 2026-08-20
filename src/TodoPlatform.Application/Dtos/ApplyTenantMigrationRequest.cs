namespace TodoPlatform.Application.Dtos;

/// <summary>
/// Optional target version for <c>POST .../migrations/apply</c>.
/// Omit to apply the next pending step on the tenant's track.
/// </summary>
public sealed record ApplyTenantMigrationRequest(long? TargetVersion = null);
