namespace TodoPlatform.Application.Dtos;

/// <summary>
/// Optional apply parameters for <c>POST .../migrations/apply</c>.
/// </summary>
public sealed record ApplyTenantMigrationRequest(
    long? TargetVersion = null,
    DateTimeOffset? ExpectedUpdatedAt = null);
