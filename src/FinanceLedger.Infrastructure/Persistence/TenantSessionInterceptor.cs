using System.Data.Common;
using FinanceLedger.Application.Common;
using FinanceLedger.Application.Interfaces;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FinanceLedger.Infrastructure.Persistence;

/// <summary>
/// Stamps every database connection this app opens with the caller's site, for Postgres's
/// row-level security policies (supabase/migrations/0005_row_level_security.sql) to read.
/// Runs on every logical connection open - including a pooled physical connection handed
/// back to a different request - so a stale value can never linger into the wrong request.
/// Harmless against the pre-migration-0005 / non-RLS connection role: the two `set_config`
/// calls just have no policy to affect yet.
/// </summary>
public sealed class TenantSessionInterceptor : DbConnectionInterceptor
{
    private readonly ITenantProvider _tenantProvider;

    public TenantSessionInterceptor(ITenantProvider tenantProvider)
    {
        _tenantProvider = tenantProvider;
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        Apply(connection);
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        await ApplyAsync(connection, cancellationToken);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    private void Apply(DbConnection connection)
    {
        using var command = BuildCommand(connection);
        command.ExecuteNonQuery();
    }

    private async Task ApplyAsync(DbConnection connection, CancellationToken ct)
    {
        await using var command = BuildCommand(connection);
        await command.ExecuteNonQueryAsync(ct);
    }

    private DbCommand BuildCommand(DbConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText = "select set_config('app.tenant_id', @tenant, false), set_config('app.bypass_rls', @bypass, false)";

        var tenant = command.CreateParameter();
        tenant.ParameterName = "tenant";
        tenant.Value = _tenantProvider.TenantId ?? string.Empty;
        command.Parameters.Add(tenant);

        var bypass = command.CreateParameter();
        bypass.ParameterName = "bypass";
        bypass.Value = TenantBypassContext.IsBypassing ? "on" : "off";
        command.Parameters.Add(bypass);

        return command;
    }
}
