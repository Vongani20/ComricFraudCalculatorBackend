# Deploy published .NET app + hosted SPA to an existing Linux App Service (zip deploy).
#
# Usage (Microsoft email/password login):
#   .\scripts\deploy-appservice-zip.ps1 -ResourceGroup "COMRIC_POC_RG" -WebAppName "ComricWA" `
#     -UiPath "C:\Users\vmaluleke\source\repos\ComricFraudCalculatorUI"

param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroup,

    [Parameter(Mandatory = $true)]
    [string]$WebAppName,

    [string]$UiPath = "",

    [string]$TenantId = "2f37307c-b165-45ea-a717-caf228c409ab",
    [string]$SpaClientId = "c448a344-2295-4c0c-bdc9-571a1567afa2",
    [string]$ApiClientId = "c448a344-2295-4c0c-bdc9-571a1567afa2",

    [switch]$SkipUiBuild,
    [switch]$UseDevAuth
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "Azure CLI (az) is not installed. Install it, then run: az login"
}

az account show 1>$null 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Azure CLI session expired. Opening interactive login..." -ForegroundColor Yellow
    az logout 2>$null | Out-Null
    az login --tenant "2f37307c-b165-45ea-a717-caf228c409ab" --scope "https://management.core.windows.net//.default"
    if ($LASTEXITCODE -ne 0) { throw "az login failed. Run the login command manually, then retry deploy." }
}

# Fail early if Graph/ARM still needs interactive auth (common with WAM/federation).
$probe = az webapp show --name $WebAppName --resource-group $ResourceGroup --query name -o tsv 2>&1
if ($LASTEXITCODE -ne 0 -or "$probe" -match "InteractionRequired|Integrated Windows authentication") {
    Write-Host "Azure CLI cannot call ARM. Re-login required:" -ForegroundColor Yellow
    Write-Host '  az logout' -ForegroundColor Cyan
    Write-Host '  az login --tenant "2f37307c-b165-45ea-a717-caf228c409ab" --scope "https://management.core.windows.net//.default"' -ForegroundColor Cyan
    throw "Azure CLI authentication required before deploy."
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $repoRoot "publish"
$zipPath = Join-Path $repoRoot "publish.zip"
$buildUiScript = Join-Path $PSScriptRoot "build-ui.ps1"

function New-LinuxZipFromDirectory {
    param(
        [Parameter(Mandatory = $true)][string]$SourceDirectory,
        [Parameter(Mandatory = $true)][string]$DestinationZip
    )

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    if (Test-Path $DestinationZip) { Remove-Item $DestinationZip -Force }

    $sourceRoot = (Resolve-Path $SourceDirectory).Path.TrimEnd('\', '/')
    $zip = [System.IO.Compression.ZipFile]::Open(
        $DestinationZip,
        [System.IO.Compression.ZipArchiveMode]::Create)

    try {
        Get-ChildItem -Path $sourceRoot -Recurse -File | ForEach-Object {
            $relative = $_.FullName.Substring($sourceRoot.Length + 1).Replace('\', '/')
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $zip,
                $_.FullName,
                $relative,
                [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    }
    finally {
        $zip.Dispose()
    }
}

Push-Location $repoRoot
try {
    if (-not $SkipUiBuild) {
        $uiArgs = @{
            TenantId    = $TenantId
            SpaClientId = $SpaClientId
            ApiClientId = $ApiClientId
        }
        if ($UiPath) { $uiArgs.UiPath = $UiPath }
        if ($UseDevAuth) { $uiArgs.UseDevAuth = $true }

        & $buildUiScript @uiArgs
    }
    elseif (-not (Test-Path (Join-Path $repoRoot "wwwroot\index.html"))) {
        throw "wwwroot/index.html is missing. Run .\scripts\build-ui.ps1 or omit -SkipUiBuild."
    }

    if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

    Write-Host "Publishing Release build for linux-x64 (framework-dependent)..." -ForegroundColor Cyan
    dotnet publish ComricFraudCalculatorBackend.csproj `
        -c Release `
        -r linux-x64 `
        --self-contained false `
        -o $publishDir
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

    $sqlClient = Join-Path $publishDir "Microsoft.Data.SqlClient.dll"
    if (-not (Test-Path $sqlClient)) {
        throw "Publish output is missing Microsoft.Data.SqlClient.dll at $sqlClient"
    }

    $publishedUi = Join-Path $publishDir "wwwroot\index.html"
    if (-not (Test-Path $publishedUi)) {
        throw "Publish output is missing wwwroot/index.html. UI was not included in the package."
    }

    Write-Host "Creating zip (forward-slash entries for Linux)..." -ForegroundColor Cyan
    New-LinuxZipFromDirectory -SourceDirectory $publishDir -DestinationZip $zipPath

    $audience = "api://$ApiClientId"
    if ($UseDevAuth) {
        Write-Host "Configuring App Service for Dev Auth..." -ForegroundColor Yellow
        az webapp config appsettings set `
            --resource-group $ResourceGroup `
            --name $WebAppName `
            --settings `
                LocalDevelopment__UseDevAuth=true `
                LocalDevelopment__EnableRls=false `
                WEBSITE_RUN_FROM_PACKAGE=1 `
            -o none | Out-Null
    }
    else {
        Write-Host "Configuring App Service for Microsoft Entra JWT auth..." -ForegroundColor Cyan
        az webapp config appsettings set `
            --resource-group $ResourceGroup `
            --name $WebAppName `
            --settings `
                LocalDevelopment__UseDevAuth=false `
                LocalDevelopment__EnableRls=false `
                AzureAd__Instance="https://login.microsoftonline.com/" `
                AzureAd__TenantId=$TenantId `
                AzureAd__ClientId=$ApiClientId `
                AzureAd__Audience=$audience `
                Platform__AllowedEmailDomain=solugrowth.com `
                WEBSITE_RUN_FROM_PACKAGE=1 `
            -o none | Out-Null
    }

    Write-Host "Stopping $WebAppName before zip deploy..." -ForegroundColor Cyan
    az webapp stop --resource-group $ResourceGroup --name $WebAppName | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Failed to stop $WebAppName." }
    Start-Sleep -Seconds 8

    $deployOk = $false
    try {
        # Async deploy: Azure's sync poll often sits on "Starting the site..." for several
        # minutes while EF migrations run on cold start. The package is uploaded either way.
        Write-Host "Deploying to $WebAppName (az webapp deploy --async true)..." -ForegroundColor Cyan
        az webapp deploy `
            --resource-group $ResourceGroup `
            --name $WebAppName `
            --src-path $zipPath `
            --type zip `
            --async true
        if ($LASTEXITCODE -ne 0) {
            Write-Host "az webapp deploy failed; retrying once after 15s..." -ForegroundColor Yellow
            Start-Sleep -Seconds 15
            az webapp deploy `
                --resource-group $ResourceGroup `
                --name $WebAppName `
                --src-path $zipPath `
                --type zip `
                --async true
            if ($LASTEXITCODE -ne 0) { throw "az webapp deploy failed." }
        }
        $deployOk = $true
    }
    finally {
        Write-Host "Starting $WebAppName ..." -ForegroundColor Cyan
        az webapp start --resource-group $ResourceGroup --name $WebAppName | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Failed to start $WebAppName." }
    }

    if (-not $deployOk) { throw "Deploy did not complete." }

    $url = az webapp show --name $WebAppName --resource-group $ResourceGroup --query defaultHostName -o tsv
    $siteUrl = "https://$url"
    Write-Host "Waiting for $siteUrl to respond (migrations on first boot can take 1-3 min)..." -ForegroundColor Cyan
    $ready = $false
    for ($i = 1; $i -le 36; $i++) {
        try {
            $probe = Invoke-WebRequest -Uri $siteUrl -UseBasicParsing -TimeoutSec 10
            if ($probe.StatusCode -ge 200 -and $probe.StatusCode -lt 500) {
                $ready = $true
                break
            }
        }
        catch {
            # cold start / migrations still running
        }
        Start-Sleep -Seconds 5
    }

    if ($ready) {
        Write-Host "Deployed: $siteUrl" -ForegroundColor Green
    }
    else {
        Write-Host "Package deployed and app started, but $siteUrl did not respond within 3 minutes." -ForegroundColor Yellow
        Write-Host "Check Log stream in Azure Portal or run: az webapp log tail --name $WebAppName --resource-group $ResourceGroup" -ForegroundColor Yellow
    }
    if (-not $UseDevAuth) {
        Write-Host @"
Next (Entra portal) if Sign in with Microsoft fails:
  1. App registration $SpaClientId → Authentication → Add platform → Single-page application
  2. Redirect URI: https://$url
  3. Expose an API on $ApiClientId with scopes Events.Read, Events.Write, Signals.Read, Audit.Read, Dashboard.Read
  4. API permissions on the SPA app → grant admin consent for those scopes
Then open https://$url and use Sign in with Microsoft (email + password).
"@ -ForegroundColor Yellow
    }
}
finally {
    Pop-Location
}
