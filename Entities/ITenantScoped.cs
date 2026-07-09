namespace ComricFraudCalculatorBackend.Entities;

public interface ITenantScoped
{
    Guid TenantId { get; }
}
