@description('Azure region')
param location string

@description('App Service name')
param appName string

@description('App Service plan SKU name')
param planSku string

@secure()
@description('Key Vault name holding the SQL connection string secret')
param keyVaultName string

@description('Key Vault secret name for the SQL connection string')
param connectionStringSecretName string

@description('Key Vault secret name for platform salt')
param platformSaltSecretName string

@description('Entra tenant ID')
param entraTenantId string

@description('API app registration client ID')
param entraApiClientId string

var planName = '${appName}-plan'
var webAppName = '${appName}-${uniqueString(resourceGroup().id)}'

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  sku: {
    name: planSku
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    keyVaultReferenceIdentity: 'SystemAssigned'
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=${connectionStringSecretName})'
        }
        {
          name: 'KeyVault__VaultUri'
          value: 'https://${keyVaultName}${environment().suffixes.keyvaultDns}/'
        }
        {
          name: 'Platform__Salt'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=${platformSaltSecretName})'
        }
        {
          name: 'AzureAd__Instance'
          value: 'https://login.microsoftonline.com/'
        }
        {
          name: 'AzureAd__TenantId'
          value: entraTenantId
        }
        {
          name: 'AzureAd__ClientId'
          value: entraApiClientId
        }
        {
          name: 'AzureAd__Audience'
          value: 'api://${entraApiClientId}'
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
      ]
    }
  }
}

output appServiceName string = webApp.name
output appServiceUrl string = 'https://${webApp.properties.defaultHostName}'
output managedIdentityPrincipalId string = webApp.identity.principalId
