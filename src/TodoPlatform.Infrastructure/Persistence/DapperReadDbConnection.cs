using System.Data;
using Npgsql;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Application.Tenancy;
using TodoPlatform.Infrastructure.Tenancy;

namespace TodoPlatform.Infrastructure.Persistence;

/// <summary>
/// Opens Npgsql connections for Dapper read queries and binds RLS tenant on Open.
/// </summary>
public sealed class DapperReadDbConnection(string connectionString, ITenantContext tenantContext)
    : IReadDbConnection
{
    public IDbConnection CreateConnection() =>
        new TenantBoundNpgsqlConnection(new NpgsqlConnection(connectionString), tenantContext);
}

/// <summary>Applies/resets <c>app.current_tenant</c> so pooled connections cannot leak tenant.</summary>
internal sealed class TenantBoundNpgsqlConnection(NpgsqlConnection inner, ITenantContext tenantContext) : IDbConnection
{
    [System.Diagnostics.CodeAnalysis.AllowNull]
    public string ConnectionString
    {
        get => inner.ConnectionString;
        set => inner.ConnectionString = value;
    }

    public int ConnectionTimeout => inner.ConnectionTimeout;

    public string Database => inner.Database;

    public ConnectionState State => inner.State;

    public IDbTransaction BeginTransaction() => inner.BeginTransaction();

    public IDbTransaction BeginTransaction(IsolationLevel il) => inner.BeginTransaction(il);

    public void ChangeDatabase(string databaseName) => inner.ChangeDatabase(databaseName);

    public void Close()
    {
        if (inner.State == ConnectionState.Open)
            TenantSession.Reset(inner);

        inner.Close();
    }

    public IDbCommand CreateCommand() => inner.CreateCommand();

    public void Open()
    {
        if (inner.State != ConnectionState.Open)
            inner.Open();

        TenantSession.Apply(inner, tenantContext.TenantId);
    }

    public void Dispose()
    {
        if (inner.State == ConnectionState.Open)
            TenantSession.Reset(inner);

        inner.Dispose();
    }
}
