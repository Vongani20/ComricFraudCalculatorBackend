using ComricFraudCalculatorBackend.Authorization;
using ComricFraudCalculatorBackend.Configuration;
using Microsoft.Extensions.Options;

namespace ComricFraudCalculatorBackend.Middleware;

/// <summary>
/// Rejects authenticated API calls from users outside the configured email domain.
/// </summary>
public class OrganizationEmailMiddleware(
    RequestDelegate next,
    IOptions<PlatformOptions> platformOptions)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api")
            && context.User.Identity?.IsAuthenticated == true)
        {
            var allowedDomain = platformOptions.Value.AllowedEmailDomain
                ?? OrganizationAuthorization.DefaultAllowedEmailDomain;

            if (!OrganizationAuthorization.HasAllowedEmailDomain(context.User, allowedDomain))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = $"Access is restricted to @{allowedDomain} accounts."
                });
                return;
            }
        }

        await next(context);
    }
}
