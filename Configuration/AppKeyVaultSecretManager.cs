using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace ComricFraudCalculatorBackend.Configuration;

/// <summary>
/// Maps Key Vault secrets to ASP.NET configuration keys used by App Service settings:
/// <list type="bullet">
/// <item><c>SqlConnectionString</c> → <c>ConnectionStrings:DefaultConnection</c></item>
/// <item><c>PlatformSalt</c> → <c>Platform:Salt</c></item>
/// </list>
/// </summary>
public sealed class AppKeyVaultSecretManager : KeyVaultSecretManager
{
    public const string SqlConnectionSecretName = "SqlConnectionString";
    public const string PlatformSaltSecretName = "PlatformSalt";

    public override bool Load(SecretProperties secret) =>
        secret.Name is SqlConnectionSecretName
            or PlatformSaltSecretName
            or "ConnectionStrings--DefaultConnection";

    public override string GetKey(KeyVaultSecret secret) =>
        secret.Name switch
        {
            SqlConnectionSecretName => "ConnectionStrings:DefaultConnection",
            "ConnectionStrings--DefaultConnection" => "ConnectionStrings:DefaultConnection",
            PlatformSaltSecretName => $"{PlatformOptions.SectionName}:Salt",
            _ => secret.Name.Replace("--", ConfigurationPath.KeyDelimiter)
        };
}
