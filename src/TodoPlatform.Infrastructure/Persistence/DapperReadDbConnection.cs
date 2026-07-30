using System.Data;
using Npgsql;
using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Infrastructure.Persistence;

/// <summary>
/// Opens Npgsql connections for Dapper read queries. Does not share the EF Core pool —
/// Npgsql still pools by connection string under the hood.
/// </summary>
public sealed class DapperReadDbConnection(string connectionString) : IReadDbConnection
{
    public IDbConnection CreateConnection() => new NpgsqlConnection(connectionString);
}
