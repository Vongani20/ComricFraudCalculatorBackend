using Azure.Identity;
using Microsoft.Extensions.Configuration;

namespace ComricFraudCalculatorBackend.Configuration;

public static class KeyVaultConfigurationExtensions
{
  /// <summary>
  /// Loads <c>SqlConnectionString</c> and <c>PlatformSalt</c> from Azure Key Vault when <c>KeyVault:VaultUri</c> is set.
  /// Uses <see cref="DefaultAzureCredential"/> (Visual Studio, Azure PowerShell, managed identity, browser login).
  /// </summary>
  public static IConfigurationBuilder AddAppKeyVault(this IConfigurationBuilder builder)
  {
    var interim = builder.Build();
    var vaultUri = interim["KeyVault:VaultUri"];

    if (string.IsNullOrWhiteSpace(vaultUri))
      return builder;

    builder.AddAzureKeyVault(
      new Uri(vaultUri),
      new DefaultAzureCredential(new DefaultAzureCredentialOptions
      {
        ExcludeInteractiveBrowserCredential = false
      }),
      new AppKeyVaultSecretManager());

    return builder;
  }

  public static IConfigurationBuilder AddAppConfiguration(
    this IConfigurationBuilder builder,
    string? environmentName = null)
  {
    environmentName ??= Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

    builder
      .SetBasePath(Directory.GetCurrentDirectory())
      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
      .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
      .AddEnvironmentVariables();

    builder.AddAppKeyVault();
    return builder;
  }

  public static void EnsureSqlConnectionConfigured(IConfiguration configuration, string environmentName)
  {
    if (string.Equals(environmentName, "Testing", StringComparison.OrdinalIgnoreCase))
      return;

    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrWhiteSpace(connectionString))
      return;

    var vaultUri = configuration["KeyVault:VaultUri"];
    throw new InvalidOperationException(
      string.IsNullOrWhiteSpace(vaultUri)
        ? "ConnectionStrings:DefaultConnection is missing. Set it in appsettings, or set KeyVault:VaultUri to load SqlConnectionString from Key Vault."
        : $"ConnectionStrings:DefaultConnection was not loaded from Key Vault ({vaultUri}). " +
          "Ensure secret 'SqlConnectionString' exists and your identity has 'Key Vault Secrets User' on the vault.");
  }
}
