using Microsoft.EntityFrameworkCore;
using TodoPlatform.Application.Exceptions;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Migrations;
using TodoPlatform.Domain.Tenancy;
using TodoPlatform.Infrastructure.Persistence;

namespace TodoPlatform.Infrastructure.Migrations;

/// <summary>
/// B-12.7 — beta migrations require beta track; beta + existing todos = simulated incompatibility.
/// </summary>
public sealed class TenantMigrationCompatibilityValidator(AppDbContext db)
    : ITenantMigrationCompatibilityValidator
{
    public async Task ValidateAsync(
        Guid tenantId,
        string track,
        MigrationInfo nextMigration,
        CancellationToken cancellationToken = default)
    {
        if (!nextMigration.IsBeta)
            return;

        if (!string.Equals(track, MigrationTracks.Beta, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException(
                $"Migration {nextMigration.SchemaVersionLabel} is beta-tagged and requires "
                + $"the tenant to be on track '{MigrationTracks.Beta}'.");
        }

        var hasTodos = await db.Todos.AsNoTracking()
            .AnyAsync(t => t.TenantId == tenantId, cancellationToken);

        if (hasTodos)
        {
            throw new ConflictException(
                $"Migration {nextMigration.SchemaVersionLabel} is incompatible: tenant has existing todos "
                + "(simulated beta DDL conflict). Clear or migrate data before applying.");
        }
    }
}
