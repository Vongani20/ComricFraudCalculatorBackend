using System.Security.Claims;
using ComricFraudCalculatorBackend.Services;

namespace ComricFraudCalculatorBackend.Middleware;

public class ActivityLoggingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IActivityLogService activityLogService)
    {
        await next(context);

        if (context.User.Identity?.IsAuthenticated != true)
            return;

        if (!context.Request.Path.StartsWithSegments("/api/v1"))
            return;

        try
        {
            var action = context.Request.Path.Value ?? "/";
            var clientIp = context.Connection.RemoteIpAddress?.ToString();

            await activityLogService.LogAsync(
                action,
                context.Request.Path,
                context.Request.Method,
                context.Response.StatusCode,
                clientIp,
                context.RequestAborted);
        }
        catch
        {
            // Activity logging must not fail API responses (e.g. RLS edge cases).
        }
    }
}

public static class ScopeAuthorization
{
    public static bool HasScope(ClaimsPrincipal user, string scope)
    {
        var scopes = user.FindFirst("scp")?.Value
            ?? user.FindFirst("http://schemas.microsoft.com/identity/claims/scope")?.Value
            ?? string.Empty;

        if (scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Any(s => s.Equals(scope, StringComparison.OrdinalIgnoreCase)))
            return true;

        return user.FindAll("roles")
            .Any(c => c.Value.Equals(scope, StringComparison.OrdinalIgnoreCase));
    }
}
