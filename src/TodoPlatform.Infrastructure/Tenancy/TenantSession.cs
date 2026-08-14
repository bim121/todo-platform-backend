using System.Data;
using System.Data.Common;
using Npgsql;

namespace TodoPlatform.Infrastructure.Tenancy;

/// <summary>
/// Sets PostgreSQL <c>app.current_tenant</c> used by RLS policies.
/// Session-level (not LOCAL) so it works outside a transaction; RESET on connection close
/// so Npgsql pooling cannot leak a tenant to the next request.
/// </summary>
public static class TenantSession
{
    public const string SettingName = "app.current_tenant";

    public static void Apply(IDbConnection connection, Guid tenantId)
    {
        if (!CanConfigure(connection) || tenantId == Guid.Empty)
            return;

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT set_config('app.current_tenant', @tenantId, false);";
        AddParameter(command, "tenantId", tenantId.ToString());
        command.ExecuteNonQuery();
    }

    public static async Task ApplyAsync(DbConnection connection, Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (!CanConfigure(connection) || tenantId == Guid.Empty)
            return;

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT set_config('app.current_tenant', @tenantId, false);";
        AddParameter(command, "tenantId", tenantId.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public static void Reset(IDbConnection connection)
    {
        if (!CanConfigure(connection))
            return;

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT set_config('app.current_tenant', '', false);";
        command.ExecuteNonQuery();
    }

    public static async Task ResetAsync(DbConnection connection, CancellationToken cancellationToken = default)
    {
        if (!CanConfigure(connection))
            return;

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT set_config('app.current_tenant', '', false);";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static bool CanConfigure(IDbConnection connection) =>
        connection is NpgsqlConnection { State: ConnectionState.Open };

    private static void AddParameter(IDbCommand command, string name, string value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
