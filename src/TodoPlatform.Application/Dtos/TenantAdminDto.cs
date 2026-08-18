namespace TodoPlatform.Application.Dtos;

/// <summary>
/// Admin tenant summary aligned with contracts/openapi.yaml (TenantAdminDto).
/// <c>deploymentTrack</c>: B-12 <c>stable</c>/<c>beta</c>; B-28 also <c>blue</c>/<c>green</c>.
/// </summary>
public sealed record TenantAdminDto(
    string Id,
    string Name,
    string SchemaVersion,
    string DeploymentTrack,
    string AppVersion,
    string Status);
