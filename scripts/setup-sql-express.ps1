#Requires -Version 5.1
<#
.SYNOPSIS
  Applies EF migrations to local SQL Express (Windows Authentication).

.PREREQUISITES
  - SQL Server Express service running (Services -> SQL Server (SQLEXPRESS) -> Start)
  - Connect in SSMS: PTA-ITO-LT-114\SQLEXPRESS, Windows Auth

.USAGE
  .\scripts\setup-sql-express.ps1
  .\scripts\setup-sql-express.ps1 -ApplyRls
  dotnet run
#>
param(
    [string]$Server = "PTA-ITO-LT-114\SQLEXPRESS",
    [switch]$ApplyRls
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
$database = "ComricFraudCalculator"
$connectionString = "Server=$Server;Database=$database;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Encrypt=True"

Write-Host "Checking SQL Express service..." -ForegroundColor Cyan
$service = Get-Service -Name "MSSQL`$SQLEXPRESS" -ErrorAction SilentlyContinue
if ($service -and $service.Status -ne "Running") {
    Write-Warning "SQL Server (SQLEXPRESS) is stopped. Start it in services.msc or run as admin:"
    Write-Host "  Start-Service MSSQL`$SQLEXPRESS" -ForegroundColor Yellow
}

Write-Host "Applying EF Core migrations to $Server..." -ForegroundColor Cyan
Push-Location $Root
try {
    dotnet ef database update --connection $connectionString

    if ($ApplyRls) {
        Write-Host "Applying RLS policies..." -ForegroundColor Cyan
        Write-Host "Set LocalDevelopment:EnableRls = true in appsettings.Development.json" -ForegroundColor Yellow
        sqlcmd -S $Server -E -C -d $database -i "$Root\SQL\RlsPolicies.sql"
    }
    else {
        Write-Host "Skipping RLS (local dev). Tenant isolation is enforced in app code." -ForegroundColor Cyan
        if (Get-Command sqlcmd -ErrorAction SilentlyContinue) {
            sqlcmd -S $Server -E -C -d $database -i "$Root\SQL\DropRlsPolicies.sql" 2>$null
        }
    }

    Write-Host ""
    Write-Host "SQL Express ready." -ForegroundColor Green
    Write-Host "  Server:   $Server"
    Write-Host "  Database: $database"
    Write-Host "  Auth:     Windows Authentication (your Windows login)"
    Write-Host ""
    Write-Host "Run API:    dotnet run" -ForegroundColor Green
    Write-Host "Dev token:  Authorization: Bearer dev-token" -ForegroundColor Green
    Write-Host "HTTP file:  ComricFraudCalculatorBackend.Dev.http" -ForegroundColor Green
}
finally {
    Pop-Location
}
