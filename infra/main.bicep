targetScope = 'resourceGroup'

@description('Azure region for all resources')
param location string = resourceGroup().location

@description('Base name prefix for resources')
param appName string = 'comric-fraud-api'

@description('SQL Server administrator login')
param sqlAdminLogin string

@secure()
@description('SQL Server administrator password')
param sqlAdminPassword string

@description('Microsoft Entra tenant ID')
param entraTenantId string

@description('API app registration client ID')
param entraApiClientId string

@secure()
@description('Platform salt for ID number hashing')
param platformSalt string

@description('App Service plan SKU')
param appServicePlanSku string = 'B1'

var keyVaultName = take('${replace(appName, '-', '')}kv${uniqueString(resourceGroup().id)}', 24)

module sql 'modules/sql.bicep' = {
  name: 'sql-deployment'
  params: {
    location: location
    serverName: '${appName}-sql-${uniqueString(resourceGroup().id)}'
    databaseName: 'COMRIC_DB'
    adminLogin: sqlAdminLogin
    adminPassword: sqlAdminPassword
  }
}

module keyVault 'modules/keyVault.bicep' = {
  name: 'keyvault-deployment'
  params: {
    location: location
    vaultName: keyVaultName
    sqlConnectionString: sql.outputs.connectionString
    platformSalt: platformSalt
  }
}

module appService 'modules/appService.bicep' = {
  name: 'app-service-deployment'
  params: {
    location: location
    appName: appName
    planSku: appServicePlanSku
    keyVaultName: keyVault.outputs.vaultName
    connectionStringSecretName: keyVault.outputs.connectionStringSecretName
    platformSaltSecretName: keyVault.outputs.platformSaltSecretName
    entraTenantId: entraTenantId
    entraApiClientId: entraApiClientId
  }
}

module keyVaultAccess 'modules/keyVaultAccess.bicep' = {
  name: 'keyvault-access'
  params: {
    keyVaultName: keyVault.outputs.vaultName
    principalId: appService.outputs.managedIdentityPrincipalId
  }
}

output appServiceName string = appService.outputs.appServiceName
output appServiceUrl string = appService.outputs.appServiceUrl
output sqlServerFqdn string = sql.outputs.serverFqdn
output sqlDatabaseName string = sql.outputs.databaseName
output keyVaultUri string = keyVault.outputs.vaultUri
output keyVaultName string = keyVault.outputs.vaultName
