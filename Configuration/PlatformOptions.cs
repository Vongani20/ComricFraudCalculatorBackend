namespace ComricFraudCalculatorBackend.Configuration;

public class PlatformOptions
{
    public const string SectionName = "Platform";

    /// <summary>
    /// Platform-wide salt for ID number hashing (Key Vault secret: PlatformSalt).
    /// </summary>
    public string? Salt { get; set; }
}
