# One-time setup: wire GitHub Actions auto-deploy (push to main → ComricWA).
#
# Prerequisites:
#   - Azure CLI logged in (az login)
#   - GitHub CLI (gh) authenticated to Vongani20/ComricFraudCalculatorBackend
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

Write-Host "Downloading publish profile for $WebAppName ..." -ForegroundColor Cyan
$profileXml = az webapp deployment list-publishing-profiles `
    --name $WebAppName `
    --resource-group $ResourceGroup `
    --xml
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($profileXml)) {
    throw "Failed to download publish profile."
}

$hasGh = [bool](Get-Command gh -ErrorAction SilentlyContinue)
if ($hasGh) {
    Write-Host "Setting GitHub secret AZURE_WEBAPP_PUBLISH_PROFILE on $GitHubRepo ..." -ForegroundColor Cyan
    $profileXml | gh secret set AZURE_WEBAPP_PUBLISH_PROFILE --repo $GitHubRepo
    if ($LASTEXITCODE -ne 0) {
        throw "gh secret set failed. Run: gh auth login"
    }
    Write-Host "Secret AZURE_WEBAPP_PUBLISH_PROFILE saved." -ForegroundColor Green
}
else {
    $outFile = Join-Path $env:TEMP "$WebAppName-publishProfile.xml"
    Set-Content -Path $outFile -Value $profileXml -Encoding utf8
    Write-Host @"
GitHub CLI (gh) not found. Add the secret manually:

  1. Open https://github.com/$GitHubRepo/settings/secrets/actions
  2. New repository secret
     Name:  AZURE_WEBAPP_PUBLISH_PROFILE
     Value: contents of $outFile

Then delete $outFile (it contains credentials).
"@ -ForegroundColor Yellow
}

Write-Host @"

If ComricFraudCalculatorUI is a private repo, also add secret UI_REPO_TOKEN:
  - Create a classic PAT with 'repo' scope (or fine-grained: read Contents on the UI repo)
  - GitHub → Settings → Secrets → Actions → UI_REPO_TOKEN

Commit and push the workflow on main:
  git add .github/workflows/deploy-appservice.yml scripts/setup-github-actions-deploy.ps1
  git commit -m "ci: deploy App Service on push to main"
  git push origin main

After that, every push to main builds UI+API and deploys to $WebAppName.
"@ -ForegroundColor Cyan
