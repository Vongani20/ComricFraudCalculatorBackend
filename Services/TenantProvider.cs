using System.Security.Claims;

namespace ComricFraudCalculatorBackend.Services;

public class TenantProvider(IHttpContextAccessor httpContextAccessor) : ITenantProvider
{
    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

    public Guid? GetTenantId()
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

    public string? GetTenantCode() =>
        httpContextAccessor.HttpContext?.User?.FindFirst("tenant_code")?.Value;
}
