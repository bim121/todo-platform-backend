namespace TodoPlatform.Application.Dtos;

/// <summary>
/// Admin tenant summary aligned with contracts/openapi.yaml (TenantAdminDto).
/// </summary>
public sealed record TenantAdminDto(
    string Id,
    string Name,
    string SchemaVersion,
    string DeploymentTrack,
    string AppVersion,
    string Status);
