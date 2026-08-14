using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TodoPlatform.Application.Tenancy;

namespace TodoPlatform.Infrastructure.Tenancy;

/// <summary>
/// B-11.3 — SET <c>app.current_tenant</c> when EF opens a Postgres connection.
/// </summary>
public sealed class TenantDbConnectionInterceptor(ITenantContext tenantContext) : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        TenantSession.Apply(connection, tenantContext.TenantId);
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await TenantSession.ApplyAsync(connection, tenantContext.TenantId, cancellationToken);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    public override InterceptionResult ConnectionClosing(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result)
    {
        TenantSession.Reset(connection);
        return base.ConnectionClosing(connection, eventData, result);
    }

    public override async ValueTask<InterceptionResult> ConnectionClosingAsync(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result)
    {
        await TenantSession.ResetAsync(connection);
        return await base.ConnectionClosingAsync(connection, eventData, result);
    }
}
