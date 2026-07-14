using 'main.bicep'

param appName = 'comric-fraud-api-dev'
param sqlAdminLogin = 'sqladmin'
param sqlAdminPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD', 'ChangeMe!123')
param platformSalt = readEnvironmentVariable('PLATFORM_SALT', 'ChangeMe!DevSalt')
param entraTenantId = readEnvironmentVariable('ENTRA_TENANT_ID', 'YOUR_ENTRA_TENANT_ID')
param entraApiClientId = readEnvironmentVariable('ENTRA_API_CLIENT_ID', 'YOUR_API_CLIENT_ID')
param appServicePlanSku = 'B1'
