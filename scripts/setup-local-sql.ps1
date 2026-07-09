#Requires -Version 5.1
<#
.SYNOPSIS
  Starts local SQL Server (Docker) and applies EF Core migrations.

.USAGE
  .\scripts\setup-local-sql.ps1
  .\scripts\setup-local-sql.ps1 -ApplyRls
  dotnet run
#>
param(
    [switch]$ApplyRls
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot

Write-Host "Starting SQL Server container..." -ForegroundColor Cyan
Push-Location $Root
try {
    docker compose up -d sqlserver

    Write-Host "Waiting for SQL Server to accept connections..." -ForegroundColor Cyan
    $connected = $false
    for ($i = 0; $i -lt 30; $i++) {
        docker exec comric-fraud-sql /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "ComricDev!2026" -C -Q "SELECT 1" 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) { $connected = $true; break }
        Start-Sleep -Seconds 3
    }
    if (-not $connected) {
        throw "SQL Server did not become ready in time. Check: docker logs comric-fraud-sql"
    }

    $connectionString = "Server=localhost,1433;Database=ComricFraudCalculator;User Id=sa;Password=ComricDev!2026;TrustServerCertificate=True;MultipleActiveResultSets=true"

    Write-Host "Applying EF Core migrations..." -ForegroundColor Cyan
    dotnet ef database update --project $Root --connection $connectionString

    if ($ApplyRls) {
        Write-Host "Applying RLS policies (set LocalDevelopment:EnableRls=true in appsettings.Development.json)..." -ForegroundColor Cyan
        docker cp "$Root\SQL\RlsPolicies.sql" comric-fraud-sql:/tmp/RlsPolicies.sql
        docker exec comric-fraud-sql /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "ComricDev!2026" -C -d ComricFraudCalculator -i /tmp/RlsPolicies.sql
    }
    else {
        Write-Host "Ensuring RLS policies are not active for local dev..." -ForegroundColor Cyan
        docker cp "$Root\SQL\DropRlsPolicies.sql" comric-fraud-sql:/tmp/DropRlsPolicies.sql
        docker exec comric-fraud-sql /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "ComricDev!2026" -C -d ComricFraudCalculator -i /tmp/DropRlsPolicies.sql
    }

    Write-Host ""
    Write-Host "Local SQL ready." -ForegroundColor Green
    Write-Host "  Server:   localhost,1433"
    Write-Host "  Database: ComricFraudCalculator"
    Write-Host "  Auth:     Authorization: Bearer dev-token"
    Write-Host ""
    Write-Host "Run the API:  dotnet run" -ForegroundColor Green
    Write-Host "Test file:    ComricFraudCalculatorBackend.Dev.http" -ForegroundColor Green
}
finally {
    Pop-Location
}
