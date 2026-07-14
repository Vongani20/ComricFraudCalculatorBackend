# Apply EF Core migrations using SqlConnectionString from Azure Key Vault (cmckv).
# Prerequisites:
#   - Secret SqlConnectionString exists in the vault
#   - Sign in via Visual Studio Azure account, or Connect-AzAccount / interactive browser
#
# Usage:
#   .\scripts\apply-migrations-azure.ps1
#   .\scripts\apply-migrations-azure.ps1 -KeyVaultUri "https://cmckv.vault.azure.net/"

param(
    [string]$KeyVaultUri = "https://cmckv.vault.azure.net/"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

try {
    Get-Process ComricFraudCalculatorBackend -ErrorAction SilentlyContinue | Stop-Process -Force

    $env:ASPNETCORE_ENVIRONMENT = "Production"
    $env:KeyVault__VaultUri = $KeyVaultUri
    # Clear any leftover local override so Key Vault wins
    Remove-Item Env:\ConnectionStrings__DefaultConnection -ErrorAction SilentlyContinue

    Write-Host "Applying migrations using Key Vault ($KeyVaultUri) secret SqlConnectionString..." -ForegroundColor Cyan
    dotnet ef database update --project ComricFraudCalculatorBackend.csproj

    Write-Host "Done. Tables: Tenants, HrEvents, MnoEvents, Signals, ActivityLogs" -ForegroundColor Green
}
finally {
    Pop-Location
}
