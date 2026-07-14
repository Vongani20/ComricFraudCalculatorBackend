# Apply EF Core migrations to local SQL Server (localhost).

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

try {
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    Write-Host "Running migrations against local SQL..." -ForegroundColor Cyan
    dotnet ef database update --project ComricFraudCalculatorBackend.csproj
    Write-Host "Done." -ForegroundColor Green
}
finally {
    Pop-Location
}
