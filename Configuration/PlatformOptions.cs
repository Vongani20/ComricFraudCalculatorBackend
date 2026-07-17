namespace ComricFraudCalculatorBackend.Configuration;

public class PlatformOptions
{
    public const string SectionName = "Platform";

    /// <summary>
    /// Platform-wide salt for ID number hashing (Key Vault secret: PlatformSalt).
    /// </summary>
    public string? Salt { get; set; }

    /// <summary>
    /// Only users with an email in this domain may call the API (e.g. solugrowth.com).
    /// </summary>
    public string? AllowedEmailDomain { get; set; } = "solugrowth.com";
}
