using System.Data;
using System.Data.Common;
using Npgsql;
using TodoPlatform.Domain.Tenancy;

namespace TodoPlatform.Infrastructure.Tenancy;

/// <summary>
/// Sets PostgreSQL GUCs used by RLS policies and tenant <c>search_path</c> (B-12.11).
/// Session-level (not LOCAL) so it works outside a transaction; RESET on connection close
/// so Npgsql pooling cannot leak a tenant to the next request.
/// </summary>
public static class TenantSession
{
    public const string SettingName = "app.current_tenant";
    public const string BypassSettingName = "app.bypass_rls";

    public static void Apply(IDbConnection connection, Guid tenantId, string? schemaName = null)
    {
        if (!CanConfigure(connection) || tenantId == Guid.Empty)
            return;

        using var command = connection.CreateCommand();
        command.CommandText = BuildApplySql(schemaName);
        AddParameter(command, "tenantId", tenantId.ToString());
        command.ExecuteNonQuery();
    }

    public static async Task ApplyAsync(
        DbConnection connection,
        Guid tenantId,
        string? schemaName = null,
        CancellationToken cancellationToken = default)
    {
        if (!CanConfigure(connection) || tenantId == Guid.Empty)
            return;

        await using var command = connection.CreateCommand();
        command.CommandText = BuildApplySql(schemaName);
        AddParameter(command, "tenantId", tenantId.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public static void ApplyBypass(IDbConnection connection)
    {
        if (!CanConfigure(connection))
            return;

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT set_config('app.bypass_rls', 'true', false);";
        command.ExecuteNonQuery();
    }

    public static void Reset(IDbConnection connection)
    {
        if (!CanConfigure(connection))
            return;

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT set_config('search_path', 'public', false),
                   set_config('app.current_tenant', '', false),
                   set_config('app.bypass_rls', '', false);
            """;
        command.ExecuteNonQuery();
    }

    public static async Task ResetAsync(DbConnection connection, CancellationToken cancellationToken = default)
    {
        if (!CanConfigure(connection))
            return;

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT set_config('search_path', 'public', false),
                   set_config('app.current_tenant', '', false),
                   set_config('app.bypass_rls', '', false);
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string BuildApplySql(string? schemaName)
    {
        if (!string.IsNullOrWhiteSpace(schemaName) && TenantSchemaNaming.IsValidSchemaName(schemaName))
        {
            return $"""
                SELECT set_config('search_path', '{schemaName}, public', false),
                       set_config('app.current_tenant', @tenantId, false);
                """;
        }

        return "SELECT set_config('app.current_tenant', @tenantId, false);";
    }

    private static bool CanConfigure(IDbConnection connection) =>
        connection.State == ConnectionState.Open
        && (connection is NpgsqlConnection || connection.GetType().Name.Contains("Npgsql", StringComparison.Ordinal));

    private static void AddParameter(IDbCommand command, string name, string value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
