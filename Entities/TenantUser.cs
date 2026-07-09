namespace ComricFraudCalculatorBackend.Entities;

public class TenantUser
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string ProfileType { get; set; } = string.Empty;
    public string ProfileJson { get; set; } = "{}";
    public string AvatarInitials { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
