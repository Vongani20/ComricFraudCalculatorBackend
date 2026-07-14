# Set / update Azure Key Vault secrets for this app (no Azure CLI required).
# Sign in with Visual Studio Azure account, or Install-Module Az.Accounts + Connect-AzAccount.
#
# Usage:
#   .\scripts\set-keyvault-secrets.ps1
#   .\scripts\set-keyvault-secrets.ps1 -SqlPassword "YourPassword"
#   .\scripts\set-keyvault-secrets.ps1 -SqlPassword $env:SQL_PASSWORD -PlatformSalt $env:PLATFORM_SALT

param(
    [string]$VaultUri = "https://cmckv.vault.azure.net/",
    [string]$SqlServer = "sql-comric-poc.database.windows.net",
    [string]$Database = "ComricFraudCalculator",
    [string]$SqlUser = "comricsa",
    [Parameter(Mandatory = $true)]
    [string]$SqlPassword,
    [string]$PlatformSalt
)

$ErrorActionPreference = "Stop"

$identityPkg = Join-Path $env:USERPROFILE ".nuget\packages\azure.identity"
$secretsPkg = Join-Path $env:USERPROFILE ".nuget\packages\azure.security.keyvault.secrets"
$corePkg = Join-Path $env:USERPROFILE ".nuget\packages\azure.core"

if (-not (Test-Path $secretsPkg)) {
    Write-Host "Restoring NuGet packages so Azure SDK assemblies are available..." -ForegroundColor Yellow
    Push-Location (Split-Path -Parent $PSScriptRoot)
    try { dotnet restore ComricFraudCalculatorBackend.csproj | Out-Null } finally { Pop-Location }
}

function Find-LatestDll([string]$packageRoot, [string]$dllName) {
    $match = Get-ChildItem -Path $packageRoot -Recurse -Filter $dllName -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match '\\lib\\net[68]' } |
        Sort-Object FullName -Descending |
        Select-Object -First 1
    if (-not $match) { throw "Could not find $dllName under $packageRoot. Run: dotnet restore" }
    return $match.FullName
}

Add-Type -Path (Find-LatestDll $corePkg "Azure.Core.dll")
Add-Type -Path (Find-LatestDll $identityPkg "Azure.Identity.dll")
Add-Type -Path (Find-LatestDll $secretsPkg "Azure.Security.KeyVault.Secrets.dll")

$credential = [Azure.Identity.DefaultAzureCredential]::new(
    [Azure.Identity.DefaultAzureCredentialOptions]@{ ExcludeInteractiveBrowserCredential = $false })
$client = [Azure.Security.KeyVault.Secrets.SecretClient]::new([Uri]$VaultUri, $credential)

$connectionString = "Server=tcp:$SqlServer,1433;Initial Catalog=$Database;User ID=$SqlUser;Password=$SqlPassword;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=true;Connection Timeout=30;"

Write-Host "Setting secret SqlConnectionString in $VaultUri ..." -ForegroundColor Cyan
[void]$client.SetSecret("SqlConnectionString", $connectionString)
Write-Host "SqlConnectionString updated." -ForegroundColor Green

if (-not [string]::IsNullOrWhiteSpace($PlatformSalt)) {
    Write-Host "Setting secret PlatformSalt ..." -ForegroundColor Cyan
    [void]$client.SetSecret("PlatformSalt", $PlatformSalt)
    Write-Host "PlatformSalt updated." -ForegroundColor Green
}

Write-Host ""
Write-Host "App Service already uses:" -ForegroundColor Cyan
Write-Host "  ConnectionStrings__DefaultConnection = @Microsoft.KeyVault(SecretUri=https://cmckv.vault.azure.net/secrets/SqlConnectionString)"
Write-Host "  Platform__Salt = @Microsoft.KeyVault(SecretUri=https://cmckv.vault.azure.net/secrets/PlatformSalt)"
Write-Host ""
Write-Host "Restart the App Service after updating secrets." -ForegroundColor Yellow
Write-Host "Local (Key Vault):  dotnet run --launch-profile azure" -ForegroundColor Cyan
Write-Host "Migrations:         .\scripts\apply-migrations-azure.ps1" -ForegroundColor Cyan
