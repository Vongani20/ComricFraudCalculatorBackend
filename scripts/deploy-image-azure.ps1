# Deploy Docker image to Azure App Service (Web App for Containers) via Azure Container Registry.
#
# Prerequisites:
#   - Azure CLI installed and logged in (az login)
#   - Docker Desktop running
#   - Local image built: docker build -t comricfraudcalculatorbackend .
#
# Usage:
#   .\scripts\deploy-image-azure.ps1 -ResourceGroup "rg-comric-fraud-dev" -WebAppName "your-app-name"
#   .\scripts\deploy-image-azure.ps1 -ResourceGroup "rg-..." -WebAppName "..." -AcrName "comricfraudacr"

param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroup,

    [Parameter(Mandatory = $true)]
    [string]$WebAppName,

    [string]$AcrName = "",
    [string]$ImageName = "comricfraudcalculatorbackend",
    [string]$ImageTag = "latest",
    [string]$Location = "southafricanorth"
)

$ErrorActionPreference = "Continue"

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "Azure CLI (az) is not installed. Install from https://learn.microsoft.com/cli/azure/install-azure-cli-windows then run: az login"
}

az account show 1>$null 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Signing in to Azure..." -ForegroundColor Cyan
    az login | Out-Null
}

$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

try {
    if (-not $AcrName) {
        # Stable name (3–50 alphanumeric) so re-runs reuse the same registry
        $AcrName = "comricpocacr"
        Write-Host "Using ACR: $AcrName" -ForegroundColor Cyan
    }

    $acrExists = $null
    try {
        $acrExists = az acr show -n $AcrName -g $ResourceGroup --query name -o tsv 2>$null
        if ($LASTEXITCODE -ne 0) { $acrExists = $null }
    } catch {
        $acrExists = $null
    }

    if ([string]::IsNullOrWhiteSpace($acrExists)) {
        Write-Host "Creating Azure Container Registry '$AcrName'..." -ForegroundColor Cyan
        $rgLocation = az group show -n $ResourceGroup --query location -o tsv 2>$null
        if (-not [string]::IsNullOrWhiteSpace($rgLocation)) { $Location = $rgLocation }
        az acr create -n $AcrName -g $ResourceGroup --sku Basic --admin-enabled true --location $Location --only-show-errors 1>$null
        if ($LASTEXITCODE -ne 0) { throw "Failed to create ACR '$AcrName'." }
    }

    $loginServer = az acr show -n $AcrName -g $ResourceGroup --query loginServer -o tsv --only-show-errors
    $acrUser = az acr credential show -n $AcrName -g $ResourceGroup --query username -o tsv --only-show-errors
    $acrPass = az acr credential show -n $AcrName -g $ResourceGroup --query "passwords[0].value" -o tsv --only-show-errors

    if ([string]::IsNullOrWhiteSpace($loginServer) -or [string]::IsNullOrWhiteSpace($acrPass)) {
        throw "Could not read ACR credentials for '$AcrName'."
    }

    Write-Host "Logging into ACR $loginServer ..." -ForegroundColor Cyan
    $acrPass | docker login $loginServer -u $acrUser --password-stdin 2>&1 | Out-Null
    $acrPass = $null

    $remoteImage = "${loginServer}/${ImageName}:${ImageTag}"
    Write-Host "Building and tagging image as $remoteImage ..." -ForegroundColor Cyan
    docker build -t $ImageName .
    if ($LASTEXITCODE -ne 0) { throw "docker build failed." }
    docker tag "${ImageName}:latest" $remoteImage

    Write-Host "Pushing image..." -ForegroundColor Cyan
    docker push $remoteImage
    if ($LASTEXITCODE -ne 0) { throw "docker push failed." }

    # Re-fetch password only for App Service config (not printed)
    $acrPass = az acr credential show -n $AcrName -g $ResourceGroup --query "passwords[0].value" -o tsv --only-show-errors

    Write-Host "Configuring App Service '$WebAppName' to run the container..." -ForegroundColor Cyan
    az webapp config container set `
        --name $WebAppName `
        --resource-group $ResourceGroup `
        --docker-custom-image-name $remoteImage `
        --docker-registry-server-url "https://$loginServer" `
        --docker-registry-server-user $acrUser `
        --docker-registry-server-password $acrPass `
        --only-show-errors 1>$null

    $acrPass = $null

    az webapp config appsettings set `
        --name $WebAppName `
        --resource-group $ResourceGroup `
        --settings WEBSITES_PORT=8080 `
        --only-show-errors 1>$null

    Write-Host "Restarting App Service..." -ForegroundColor Cyan
    az webapp restart --name $WebAppName --resource-group $ResourceGroup --only-show-errors 1>$null

    $url = az webapp show --name $WebAppName --resource-group $ResourceGroup --query defaultHostName -o tsv --only-show-errors
    Write-Host ""
    Write-Host "Deployed: https://$url" -ForegroundColor Green
    Write-Host "Image:    $remoteImage" -ForegroundColor Green
    Write-Host "Ensure Key Vault app settings (SqlConnectionString, PlatformSalt) are still configured." -ForegroundColor Yellow
}
finally {
    Pop-Location
}
