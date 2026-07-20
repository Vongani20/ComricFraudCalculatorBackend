namespace ComricFraudCalculatorBackend.Services;

public class TenantProvider(IHttpContextAccessor httpContextAccessor) : ITenantProvider
{
    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

    public Guid? GetTenantId() => TenantContextResolver.Resolve(httpContextAccessor.HttpContext);

    public string? GetTenantCode() =>
        TenantContextResolver.ResolveTenantCode(httpContextAccessor.HttpContext?.User);
}
