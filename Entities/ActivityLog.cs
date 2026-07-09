namespace ComricFraudCalculatorBackend.Entities;

public class ActivityLog : ITenantScoped
{
    public Guid ActivityLogId { get; set; }
    public Guid TenantId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string? ClientIp { get; set; }
    public DateTime CreatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
