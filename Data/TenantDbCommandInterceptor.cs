using System.Data;
using System.Data.Common;
using System.Security.Claims;
using ComricFraudCalculatorBackend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ComricFraudCalculatorBackend.Data;

/// <summary>
/// Sets SESSION_CONTEXT before each command. Required because pooled connections
/// do not always re-fire ConnectionOpened, so RLS would see a null tenant.
/// </summary>
public class TenantDbCommandInterceptor(IHttpContextAccessor httpContextAccessor) : DbCommandInterceptor
{
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        SetTenantContext(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        SetTenantContext(command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        SetTenantContext(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        SetTenantContext(command);
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        SetTenantContext(command);
        return base.ScalarExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        SetTenantContext(command);
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
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

    private void SetTenantContext(DbCommand command)
    {
        var tenantId = GetTenantId();
        if (tenantId is null)
            return;

        var connection = command.Connection;
        if (connection is null)
            return;

        if (connection.State != ConnectionState.Open)
            connection.Open();

        using var contextCommand = connection.CreateCommand();
        contextCommand.Transaction = command.Transaction;
        contextCommand.CommandText = "EXEC sp_set_session_context @key = N'TenantId', @value = @tenantId, @read_only = 0;";
        var parameter = contextCommand.CreateParameter();
        parameter.ParameterName = "@tenantId";
        parameter.Value = tenantId.Value;
        contextCommand.Parameters.Add(parameter);
        contextCommand.ExecuteNonQuery();
    }
}
