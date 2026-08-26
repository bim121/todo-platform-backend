using Npgsql;
using TodoPlatform.Domain.Tenancy;

namespace TodoPlatform.Infrastructure.Migrations;

internal static class TenantConnectionStrings
{
    public static string WithSearchPath(string connectionString, string schemaName)
    {
        if (!TenantSchemaNaming.IsValidSchemaName(schemaName))
            throw new ArgumentException("Invalid tenant schema name.", nameof(schemaName));

        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Options = $"-c search_path={schemaName},public"
        };
        return builder.ConnectionString;
    }
}
