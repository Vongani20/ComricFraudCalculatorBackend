# One-time setup: wire GitHub Actions auto-deploy (push to main → ComricWA).
#
# Creates a service principal with Contributor on the resource group and stores
# it as the GitHub secret AZURE_CREDENTIALS (used by azure/login).
#
# Prerequisites:
#   - Azure CLI logged in (az login)
#   - Permission to create app registrations / assign RBAC
#   - GitHub CLI (gh) authenticated (recommended)
#
# Usage:
#   .\scripts\setup-github-actions-deploy.ps1
#   .\scripts\setup-github-actions-deploy.ps1 -ResourceGroup "COMRIC_POC_RG" -WebAppName "ComricWA"

param(
    [string]$ResourceGroup = "COMRIC_POC_RG",
    [string]$WebAppName = "ComricWA",
    [string]$GitHubRepo = "Vongani20/ComricFraudCalculatorBackend",
    [string]$SpName = "github-actions-comricwa"
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "Azure CLI (az) is required."
}

az account show 1>$null 2>$null
if ($LASTEXITCODE -ne 0) {
    throw "Run: az login --tenant 2f37307c-b165-45ea-a717-caf228c409ab"
}

$subscriptionId = az account show --query id -o tsv
if ([string]::IsNullOrWhiteSpace($subscriptionId)) {
    throw "Could not read Azure subscription id."
}

$scope = "/subscriptions/$subscriptionId/resourceGroups/$ResourceGroup"
Write-Host "Creating / resetting service principal '$SpName' with Contributor on $ResourceGroup ..." -ForegroundColor Cyan

# sdk-auth JSON is the format azure/login expects for the creds input.
$credsJson = az ad sp create-for-rbac `
    --name $SpName `
    --role Contributor `
    --scopes $scope `
    --sdk-auth `
    -o json
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($credsJson)) {
    throw "Failed to create service principal. You need Application Administrator (or similar) + Owner/User Access Administrator on the RG."
}

# Confirm the web app exists (fail early before writing secrets).
$appName = az webapp show --name $WebAppName --resource-group $ResourceGroup --query name -o tsv 2>$null
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($appName)) {
    throw "Web app $WebAppName not found in $ResourceGroup."
}

$hasGh = [bool](Get-Command gh -ErrorAction SilentlyContinue)
if ($hasGh) {
    Write-Host "Setting GitHub secret AZURE_CREDENTIALS on $GitHubRepo ..." -ForegroundColor Cyan
    $credsJson | gh secret set AZURE_CREDENTIALS --repo $GitHubRepo
    if ($LASTEXITCODE -ne 0) {
        throw "gh secret set failed. Run: gh auth login"
    }
    Write-Host "Secret AZURE_CREDENTIALS saved." -ForegroundColor Green
}
else {
    $outFile = Join-Path $env:TEMP "$SpName-azure-credentials.json"
    Set-Content -Path $outFile -Value $credsJson -Encoding utf8
    Write-Host @"
GitHub CLI (gh) not found. Add the secret manually:

  1. Open https://github.com/$GitHubRepo/settings/secrets/actions
  2. New repository secret
     Name:  AZURE_CREDENTIALS
     Value: contents of $outFile

Then delete $outFile (it contains credentials).
"@ -ForegroundColor Yellow
}

Write-Host @"

If ComricFraudCalculatorUI is a private repo, also add secret UI_REPO_TOKEN:
  - Create a classic PAT with 'repo' scope (or fine-grained: read Contents on the UI repo)
  - GitHub → Settings → Secrets → Actions → UI_REPO_TOKEN

Then re-run the workflow:
  GitHub → Actions → Deploy to App Service → Run workflow
  (or push a commit to main)
"@ -ForegroundColor Cyan
