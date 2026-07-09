namespace ComricFraudCalculatorBackend.Services;

public interface ITenantProvider
{
    Guid? GetTenantId();
    string? GetTenantCode();
    bool IsAuthenticated { get; }
}
