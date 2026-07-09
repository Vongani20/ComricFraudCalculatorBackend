# Azure Deployment (Bicep)

Deploys the Fraud Risk Assessment API to Azure App Service with Azure SQL (TDE enabled).

## Prerequisites

- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli)
- Entra app registration completed ([docs/entra-setup.md](../docs/entra-setup.md))
- .NET 10 SDK

## Deploy infrastructure

```powershell
# Login and select subscription
az login
az account set --subscription "<subscription-id>"

# Create resource group
az group create --name rg-comric-fraud-dev --location southafricanorth

# Set secrets via environment variables
$env:SQL_ADMIN_PASSWORD = "<strong-password>"
$env:ENTRA_TENANT_ID = "<tenant-id>"
$env:ENTRA_API_CLIENT_ID = "<api-client-id>"

# Deploy
az deployment group create `
  --resource-group rg-comric-fraud-dev `
  --template-file infra/main.bicep `
  --parameters infra/main.bicepparam
```

## Post-deployment steps

1. **Apply database schema**
   ```powershell
   $conn = az deployment group show -g rg-comric-fraud-dev -n main --query properties.outputs.sqlServerFqdn -o tsv
   dotnet ef database update --connection "Server=tcp:$conn,1433;Database=ComricFraudCalculator;User ID=sqladmin;Password=<password>;Encrypt=True;"
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

## Outputs

| Output | Description |
|--------|-------------|
| `appServiceUrl` | HTTPS endpoint for the API |
| `sqlServerFqdn` | Azure SQL server hostname |
| `sqlDatabaseName` | Database name (`ComricFraudCalculator`) |

## Security notes

- Store `SQL_ADMIN_PASSWORD` in Azure Key Vault for production; reference via App Service Key Vault references.
- Consider Azure SQL **Managed Identity** auth instead of SQL credentials (replace connection string with `Authentication=Active Directory Managed Identity`).
- Restrict SQL firewall to App Service outbound IPs in production.
