namespace TodoPlatform.Application.Dtos;

/// <summary>Pending migrations for a tenant track (B-12.6).</summary>
public sealed record MigrationPlanDto(
    string CurrentVersion,
    string Track,
    IReadOnlyList<MigrationPlanItemDto> Pending);

public sealed record MigrationPlanItemDto(
    long Version,
    string Description,
    IReadOnlyList<string> Tags);
