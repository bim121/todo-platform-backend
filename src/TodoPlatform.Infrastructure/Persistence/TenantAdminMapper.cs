using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Infrastructure.Persistence;

internal static class TenantAdminMapper
{
    public const string AppVersion = "1.0.0";

    public static TenantAdminDto ToDto(
        string id,
        string name,
        long currentVersion,
        string track,
        string status,
        IMigrationPlanService plans)
    {
        var label = plans.Find(currentVersion)?.SchemaVersionLabel ?? $"V{currentVersion:D3}";
        return new TenantAdminDto(
            id,
            name,
            label,
            string.IsNullOrWhiteSpace(track) ? "stable" : track,
            AppVersion,
            status.ToLowerInvariant());
    }
}
