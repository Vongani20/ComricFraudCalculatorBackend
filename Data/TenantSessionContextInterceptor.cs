using System.Data.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ComricFraudCalculatorBackend.Data;

/// <summary>
/// Sets SESSION_CONTEXT('TenantId') on each connection so Azure SQL RLS policies
/// enforce tenant isolation at the database layer.
/// </summary>
public class TenantSessionContextInterceptor(IHttpContextAccessor httpContextAccessor) : DbConnectionInterceptor
{
    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await SetTenantContextAsync(connection, cancellationToken);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        SetTenantContext(connection);
        base.ConnectionOpened(connection, eventData);
    }

    private Guid? GetTenantId()
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        var tenantClaim = user.FindFirst("tenant_id")
            ?? user.FindFirst("extension_TenantId")
            ?? user.FindFirst("tid");

        if (tenantClaim is not null && Guid.TryParse(tenantClaim.Value, out var tenantId))
            return tenantId;

        var appId = user.FindFirst("azp")?.Value ?? user.FindFirst("appid")?.Value;
        if (appId is not null && Guid.TryParse(appId, out var appTenantId))
            return appTenantId;

        return null;
    }

    private async Task SetTenantContextAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        if (tenantId is null)
            return;

        await using var command = connection.CreateCommand();
        command.CommandText = "EXEC sp_set_session_context @key = N'TenantId', @value = @tenantId, @read_only = 0;";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@tenantId";
        parameter.Value = tenantId.Value;
        command.Parameters.Add(parameter);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private void SetTenantContext(DbConnection connection)
    {
        var tenantId = GetTenantId();
        if (tenantId is null)
            return;

        using var command = connection.CreateCommand();
        command.CommandText = "EXEC sp_set_session_context @key = N'TenantId', @value = @tenantId, @read_only = 0;";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@tenantId";
        parameter.Value = tenantId.Value;
        command.Parameters.Add(parameter);
        command.ExecuteNonQuery();
    }
}
