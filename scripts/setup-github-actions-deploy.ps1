# One-time setup: wire GitHub Actions auto-deploy (push to main → ComricWA).
#
# Downloads the App Service publish profile and stores it as GitHub secret
# AZURE_WEBAPP_PUBLISH_PROFILE (used by azure/webapps-deploy).
#
# Prerequisites:
#   - Azure CLI logged in (az login)
#   - GitHub CLI (gh) authenticated (recommended), or paste the secret manually
#
# Usage:
#   .\scripts\setup-github-actions-deploy.ps1
#   .\scripts\setup-github-actions-deploy.ps1 -ResourceGroup "COMRIC_POC_RG" -WebAppName "ComricWA"

param(
    [string]$ResourceGroup = "COMRIC_POC_RG",
    [string]$WebAppName = "ComricWA",
    [string]$GitHubRepo = "Vongani20/ComricFraudCalculatorBackend"
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "Azure CLI (az) is required."
}

az account show 1>$null 2>$null
if ($LASTEXITCODE -ne 0) {
    throw "Run: az login --tenant 2f37307c-b165-45ea-a717-caf228c409ab"
}

# Publish profile deploy needs SCM basic auth enabled on Linux App Service.
Write-Host "Ensuring SCM basic auth is allowed on $WebAppName ..." -ForegroundColor Cyan
az resource update `
    --resource-group $ResourceGroup `
    --name scm `
    --namespace Microsoft.Web `
    --resource-type basicPublishingCredentialsPolicies `
    --parent "sites/$WebAppName" `
    --set properties.allow=true `
    -o none 2>$null
# Non-fatal if policy API differs by subscription; continue to download profile.

Write-Host "Downloading publish profile for $WebAppName ..." -ForegroundColor Cyan
$profileXml = az webapp deployment list-publishing-profiles `
    --name $WebAppName `
    --resource-group $ResourceGroup `
    --xml
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace("$profileXml")) {
    throw "Failed to download publish profile for $WebAppName."
}

$profileText = ($profileXml | Out-String).Trim()
if ($profileText -notmatch "<publishData>" -and $profileText -notmatch "publishProfile") {
    throw "Publish profile XML looks empty/invalid. Check App Service permissions."
}

$hasGh = [bool](Get-Command gh -ErrorAction SilentlyContinue)
if ($hasGh) {
    Write-Host "Setting GitHub secret AZURE_WEBAPP_PUBLISH_PROFILE on $GitHubRepo ..." -ForegroundColor Cyan
    $profileText | gh secret set AZURE_WEBAPP_PUBLISH_PROFILE --repo $GitHubRepo
    if ($LASTEXITCODE -ne 0) {
        throw "gh secret set failed. Run: gh auth login"
    }
    Write-Host "Secret AZURE_WEBAPP_PUBLISH_PROFILE saved." -ForegroundColor Green
}
else {
    $outFile = Join-Path $env:TEMP "$WebAppName-publishProfile.xml"
    Set-Content -Path $outFile -Value $profileText -Encoding utf8
    Write-Host @"
GitHub CLI (gh) not found. Add the secret manually:

  1. Open https://github.com/$GitHubRepo/settings/secrets/actions
  2. New repository secret
     Name:  AZURE_WEBAPP_PUBLISH_PROFILE
     Value: paste the FULL contents of:
            $outFile

Or install GitHub CLI, run 'gh auth login', then re-run this script.

Then delete $outFile (it contains credentials).
"@ -ForegroundColor Yellow
}

Write-Host @"

If ComricFraudCalculatorUI is a private repo, also add secret UI_REPO_TOKEN
(PAT with read access to that repo).

Re-run the workflow:
  GitHub → Actions → Deploy to App Service → Run workflow
"@ -ForegroundColor Cyan
