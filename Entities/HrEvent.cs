using ComricFraudCalculatorBackend.Enums;

namespace ComricFraudCalculatorBackend.Entities;

public class HrEvent : ITenantScoped
{
    public Guid EventId { get; set; }
    public Guid TenantId { get; set; }
    public string IdNumber { get; set; } = string.Empty;
    public HrEventType EventType { get; set; }
    public DateTime EventDate { get; set; }
    public string EmployerName { get; set; } = string.Empty;
    public string? EmployeeNumber { get; set; }
    public VerificationStatus VerificationStatus { get; set; }
    public int RiskScore { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
