namespace TodoPlatform.Application.Dtos;

/// <summary>Dry-run response for <c>POST .../migrations/apply?dryRun=true</c> (B-12.7).</summary>
public sealed record MigrationApplyPreviewDto(
    bool DryRun,
    string CurrentVersion,
    string Track,
    MigrationPlanItemDto? WouldApply);
