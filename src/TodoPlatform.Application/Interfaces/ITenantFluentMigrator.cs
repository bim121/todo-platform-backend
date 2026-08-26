namespace TodoPlatform.Application.Interfaces;

/// <summary>Runs FluentMigrator tenant-stream DDL inside a <c>tenant_*</c> schema (B-12.12).</summary>
public interface ITenantFluentMigrator
{
    void MigrateUp(string schemaName, long targetVersion);
}
