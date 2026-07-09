using ComricFraudCalculatorBackend.Enums;

namespace ComricFraudCalculatorBackend.Entities;

public class MnoEvent : ITenantScoped
{
    public Guid EventId { get; set; }
    public Guid TenantId { get; set; }
    public string IdNumber { get; set; } = string.Empty;
    public string Msisdn { get; set; } = string.Empty;
    public MnoEventType EventType { get; set; }
    public DateTime EventDate { get; set; }
    public ApplicationChannel ApplicationChannel { get; set; }
    public string OutletOrDealer { get; set; } = string.Empty;
    public string? DeviceImei { get; set; }
    public int RiskScore { get; set; }
    public string? FlagReason { get; set; }
    public DateTime CreatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
