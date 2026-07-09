#Requires -RunAsAdministrator
<#
  Run this script as Administrator to fix SSMS Error 26.
  Right-click PowerShell -> Run as administrator, then:
    cd C:\Users\vmaluleke\source\repos\ComricFraudCalculatorBackend
    .\scripts\start-sql-express.ps1
#>
$ErrorActionPreference = "Stop"

Write-Host "Starting SQL Server services..." -ForegroundColor Cyan

# SQL Browser is required for named instances (fixes Error 26)
Set-Service SQLBrowser -StartupType Automatic
Start-Service SQLBrowser

Set-Service MSSQL$SQLEXPRESS -StartupType Automatic
Start-Service MSSQL$SQLEXPRESS

Start-Sleep 3

Get-Service SQLBrowser, MSSQL$SQLEXPRESS | Format-Table Name, Status

Write-Host ""
Write-Host "Testing connection..." -ForegroundColor Cyan
sqlcmd -S "localhost\SQLEXPRESS" -E -C -Q "SELECT @@VERSION" -W

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "SQL Express is ready. Connect in SSMS with:" -ForegroundColor Green
    Write-Host "  Server:   localhost\SQLEXPRESS  (or PTA-ITO-LT-114\SQLEXPRESS)"
    Write-Host "  Auth:     Windows Authentication"
    Write-Host "  Encrypt:  Mandatory, Trust server certificate: checked"
    Write-Host ""
    Write-Host "Then run: .\scripts\setup-sql-express.ps1" -ForegroundColor Green
}
else {
    Write-Host "Connection still failed. Check Windows Event Viewer -> Application log for SQL Server errors." -ForegroundColor Red
}
