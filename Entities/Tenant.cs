using ComricFraudCalculatorBackend.Enums;

namespace ComricFraudCalculatorBackend.Entities;

public class Tenant
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string TenantCode { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public SubscriptionTier SubscriptionTier { get; set; } = SubscriptionTier.Basic;
    public DateTime CreatedAt { get; set; }

    public ICollection<HrEvent> HrEvents { get; set; } = [];
    public ICollection<MnoEvent> MnoEvents { get; set; } = [];
    public ICollection<ActivityLog> ActivityLogs { get; set; } = [];
}
