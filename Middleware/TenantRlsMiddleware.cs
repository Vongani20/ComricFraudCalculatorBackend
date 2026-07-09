using ComricFraudCalculatorBackend.Data;
using ComricFraudCalculatorBackend.Services;
using Microsoft.EntityFrameworkCore;

namespace ComricFraudCalculatorBackend.Middleware;

/// <summary>
/// Opens the request DbContext connection and sets SESSION_CONTEXT for RLS.
/// The connection stays open for the lifetime of the request scope.
/// </summary>
public class TenantRlsMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ApplicationDbContext db, ITenantProvider tenantProvider)
    {
        var tenantId = tenantProvider.GetTenantId();
        if (tenantId is not null)
        {
            await db.Database.OpenConnectionAsync(context.RequestAborted);

            var connection = db.Database.GetDbConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = "EXEC sp_set_session_context @key = N'TenantId', @value = @tenantId, @read_only = 0;";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@tenantId";
            parameter.Value = tenantId.Value;
            command.Parameters.Add(parameter);
            await command.ExecuteNonQueryAsync(context.RequestAborted);
        }

        await next(context);
    }
}
