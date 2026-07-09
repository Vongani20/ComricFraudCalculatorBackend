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

@description('App Service plan SKU')
param appServicePlanSku string = 'B1'

module sql 'modules/sql.bicep' = {
  name: 'sql-deployment'
  params: {
    location: location
    serverName: '${appName}-sql-${uniqueString(resourceGroup().id)}'
    databaseName: 'ComricFraudCalculator'
    adminLogin: sqlAdminLogin
    adminPassword: sqlAdminPassword
  }
}

module appService 'modules/appService.bicep' = {
  name: 'app-service-deployment'
  params: {
    location: location
    appName: appName
    planSku: appServicePlanSku
    sqlConnectionString: sql.outputs.connectionString
    entraTenantId: entraTenantId
    entraApiClientId: entraApiClientId
  }
}

output appServiceName string = appService.outputs.appServiceName
output appServiceUrl string = appService.outputs.appServiceUrl
output sqlServerFqdn string = sql.outputs.serverFqdn
output sqlDatabaseName string = sql.outputs.databaseName
