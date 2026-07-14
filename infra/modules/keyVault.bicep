@description('Azure region')
param location string

@description('Key Vault name (3-24 alphanumeric characters)')
param vaultName string

@secure()
@description('Full Azure SQL connection string including password')
param sqlConnectionString string

@secure()
@description('Platform salt for ID number hashing')
param platformSalt string

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: vaultName
  location: location
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
  }
}

resource sqlConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'SqlConnectionString'
  properties: {
    value: sqlConnectionString
  }
}

resource platformSaltSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'PlatformSalt'
  properties: {
    value: platformSalt
  }
}

output vaultUri string = keyVault.properties.vaultUri
output vaultName string = keyVault.name
output vaultId string = keyVault.id
output connectionStringSecretName string = 'SqlConnectionString'
output platformSaltSecretName string = 'PlatformSalt'
