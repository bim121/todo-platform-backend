using System.Data;

namespace TodoPlatform.Application.Interfaces;

/// <summary>
/// Factory for read-side ADO.NET connections (Dapper). Uses <c>ConnectionStrings:Read</c>
/// so the read replica can diverge from the write connection later without touching handlers.
/// </summary>
public interface IReadDbConnection
{
    IDbConnection CreateConnection();
}
