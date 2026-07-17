# Azure Deployment (Bicep)

Deploys the Fraud Risk Assessment API to Azure App Service with Azure SQL (TDE enabled) and **Key Vault** for the SQL connection string (password never stored in App Service settings).

## Prerequisites

- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli)
- Entra app registration completed ([docs/entra-setup.md](../docs/entra-setup.md))
- .NET 10 SDK
- Deploying user needs **Key Vault Administrator** (or Secrets Officer) on the subscription/resource group for the initial deployment

## Deploy infrastructure

```powershell
# Login and select subscription
az login
az account set --subscription "<subscription-id>"

# Create resource group
az group create --name rg-comric-fraud-dev --location southafricanorth

# Set secrets via environment variables (used only during Bicep deploy to seed Key Vault)
$env:SQL_ADMIN_PASSWORD = "<strong-password>"
$env:ENTRA_TENANT_ID = "<tenant-id>"
$env:ENTRA_API_CLIENT_ID = "<api-client-id>"

# Deploy
az deployment group create `
  --resource-group rg-comric-fraud-dev `
  --template-file infra/main.bicep `
  --parameters infra/main.bicepparam
```

## Key Vault connection string

Bicep creates:

| Resource | Purpose |
|----------|---------|
| **Key Vault** | Stores secret `ConnectionStrings--DefaultConnection` (full Azure SQL connection string) |
| **App Service** | Reads connection string via `@Microsoft.KeyVault(VaultName=...;SecretName=...)` |
| **Managed identity** | Granted **Key Vault Secrets User** on the vault |

The SQL password is **only** in Key Vault — not in `appsettings.json` or plain App Service settings.

## Post-deployment steps

1. **Apply database schema** (from a machine with Key Vault access):
   ```powershell
   az login
   $vaultUri = az deployment group show -g rg-comric-fraud-dev -n main --query properties.outputs.keyVaultUri.value -o tsv
   $env:ASPNETCORE_ENVIRONMENT = "Production"
   $env:KeyVault__VaultUri = $vaultUri
   dotnet ef database update
   ```

2. **Apply RLS policies** — run `SQL/RlsPolicies.sql` against the Azure SQL database.

3. **Deploy application**
   ```powershell
   dotnet publish -c Release -o ./publish
   Compress-Archive -Path ./publish/* -DestinationPath ./publish.zip -Force

   az webapp deployment source config-zip `
     --resource-group rg-comric-fraud-dev `
     --name <app-service-name-from-output> `
     --src ./publish.zip
   ```

4. **Verify** — `GET https://<app-service-url>/api/v1/fraud-signals` with a valid JWT.

## Deploy the Docker image (App Service + ACR)

```powershell
# After: az login
.\scripts\deploy-image-azure.ps1 `
  -ResourceGroup "rg-comric-fraud-dev" `
  -WebAppName "<your-app-service-name>" `
  -AcrName "<your-acr-name>"
```

This builds the image, pushes it to Azure Container Registry, and configures the App Service to run that container (`WEBSITES_PORT=8080`).

## Deploy without Docker (zip to Linux App Service)

```powershell
.\scripts\deploy-appservice-zip.ps1 `
  -ResourceGroup "rg-comric-fraud-dev" `
  -WebAppName "<your-app-service-name>"
```

## Outputs


```powershell
az login
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:KeyVault__VaultUri = "https://<your-keyvault>.vault.azure.net/"
dotnet run --launch-profile azure
```

Your Azure AD user needs **Key Vault Secrets User** on the vault.

## Outputs

| Output | Description |
|--------|-------------|
| `appServiceUrl` | HTTPS endpoint for the API |
| `sqlServerFqdn` | Azure SQL server hostname |
| `sqlDatabaseName` | Database name (`ComricFraudCalculator`) |
| `keyVaultUri` | Key Vault URI for local `KeyVault__VaultUri` |
| `keyVaultName` | Key Vault name |

## Security notes

- Rotate the SQL password by updating the Key Vault secret `ConnectionStrings--DefaultConnection`.
- Consider Azure SQL **Managed Identity** auth instead of SQL credentials for a future hardening pass.
- Restrict SQL firewall to App Service outbound IPs in production.
