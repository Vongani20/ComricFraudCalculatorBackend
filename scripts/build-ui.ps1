# Build ComricFraudCalculatorUI into this repo's wwwroot (for hosting by the API).
#
# Usage (Microsoft login for Azure):
#   .\scripts\build-ui.ps1 `
#     -UiPath "C:\...\ComricFraudCalculatorUI" `
#     -TenantId "2f37307c-..." `
#     -SpaClientId "99981c21-..." `
#     -ApiClientId "99981c21-..."
#
# Local DevAuth UI (optional):
#   .\scripts\build-ui.ps1 -UseDevAuth

param(
    [string]$UiPath = "",
    [string]$TenantId = "2f37307c-b165-45ea-a717-caf228c409ab",
    [string]$SpaClientId = "c448a344-2295-4c0c-bdc9-571a1567afa2",
    [string]$ApiClientId = "c448a344-2295-4c0c-bdc9-571a1567afa2",
    [switch]$UseDevAuth
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$wwwroot = Join-Path $repoRoot "wwwroot"

if (-not $UiPath) {
    $UiPath = Join-Path (Split-Path -Parent $repoRoot) "ComricFraudCalculatorUI"
}

if (-not (Test-Path (Join-Path $UiPath "package.json"))) {
    throw "UI project not found at '$UiPath'. Pass -UiPath to the ComricFraudCalculatorUI folder."
}

if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    throw "npm is required to build the UI. Install Node.js LTS, then retry."
}

$scopePrefix = "api://$ApiClientId"
$scopes = @(
    "$scopePrefix/Events.Read",
    "$scopePrefix/Events.Write",
    "$scopePrefix/Signals.Read",
    "$scopePrefix/Audit.Read",
    "$scopePrefix/Dashboard.Read"
) -join ","

Write-Host "Building UI from $UiPath ..." -ForegroundColor Cyan
Push-Location $UiPath
try {
    if (-not (Test-Path "node_modules")) {
        npm ci
        if ($LASTEXITCODE -ne 0) { npm install }
    }

    if ($UseDevAuth) {
        $env:VITE_USE_DEV_AUTH = "true"
        Remove-Item Env:VITE_AZURE_TENANT_ID -ErrorAction SilentlyContinue
        Remove-Item Env:VITE_AZURE_CLIENT_ID -ErrorAction SilentlyContinue
        Remove-Item Env:VITE_AZURE_API_SCOPE -ErrorAction SilentlyContinue
        Remove-Item Env:VITE_AZURE_API_SCOPES -ErrorAction SilentlyContinue
        Write-Host "Building with Dev Auth only." -ForegroundColor Yellow
    }
    else {
        $env:VITE_USE_DEV_AUTH = "false"
        $env:VITE_AZURE_TENANT_ID = $TenantId
        $env:VITE_AZURE_CLIENT_ID = $SpaClientId
        $env:VITE_AZURE_API_SCOPES = $scopes
        $env:VITE_AZURE_API_SCOPE = "$scopePrefix/Dashboard.Read"
        $env:VITE_ALLOWED_EMAIL_DOMAIN = "solugrowth.com"
        Write-Host "Building with Microsoft Entra login (tenant=$TenantId, spa=$SpaClientId)." -ForegroundColor Cyan
    }

    npm run build
    if ($LASTEXITCODE -ne 0) { throw "UI build failed." }
}
finally {
    Pop-Location
}

$dist = Join-Path $UiPath "dist"
if (-not (Test-Path (Join-Path $dist "index.html"))) {
    throw "UI build did not produce dist/index.html"
}

Write-Host "Copying UI to $wwwroot ..." -ForegroundColor Cyan
Get-ChildItem $wwwroot -Force | Where-Object { $_.Name -ne ".gitkeep" } | Remove-Item -Recurse -Force
Copy-Item -Path (Join-Path $dist "*") -Destination $wwwroot -Recurse -Force

Write-Host "UI ready in wwwroot." -ForegroundColor Green
