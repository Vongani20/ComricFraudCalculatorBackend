using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace ComricFraudCalculatorBackend.Services;

/// <summary>
/// Resolves the business (MNO/HR) tenant for RLS and event isolation.
/// Does not use Entra directory <c>tid</c> or app client id — those are not Tenants rows.
/// </summary>
public static class TenantContextResolver
{
    public const string TenantIdHeader = "X-Tenant-Id";
    public const string DevTenantIdHeader = "X-Dev-TenantId";

    public static Guid? Resolve(HttpContext? httpContext)
    {
        var user = httpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        // Explicit business-tenant claims (optional Entra optional claims / app roles later)
        var tenantClaim = user.FindFirst("tenant_id") ?? user.FindFirst("extension_TenantId");
        if (tenantClaim is not null && Guid.TryParse(tenantClaim.Value, out var fromClaim))
            return fromClaim;

        // SPA tenant switcher (Vodacom / MTN demo tenants)
        if (httpContext is not null)
        {
            var header =
                httpContext.Request.Headers[TenantIdHeader].FirstOrDefault()
                ?? httpContext.Request.Headers[DevTenantIdHeader].FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(header) && Guid.TryParse(header, out var fromHeader))
                return fromHeader;
        }

        return null;
    }

    public static string? ResolveTenantCode(ClaimsPrincipal? user) =>
        user?.FindFirst("tenant_code")?.Value;
}
