namespace TodoPlatform.Application.Dtos;

/// <summary>Apply result or dry-run preview (B-12.7).</summary>
public sealed record ApplyTenantMigrationResponse(
    bool DryRun,
    TenantAdminDto? Tenant,
    MigrationApplyPreviewDto? Preview);
