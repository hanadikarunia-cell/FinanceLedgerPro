// =============================================================================
// Key Vault secrets for Finance Ledger Pro.
// Secrets: cosmos-connection-string, blob-connection-string, jwt-secret,
//          acs-connection-string, google-sheet-id
// =============================================================================
@description('Name of an existing Key Vault.')
param keyVaultName string

@secure()
@description('Cosmos DB connection string.')
param cosmosConnectionString string

@secure()
@description('Blob storage connection string.')
param blobConnectionString string

@secure()
@description('Azure Communication Services connection string.')
param acsConnectionString string

@secure()
@description('JWT signing secret (placeholder; rotate after deploy).')
param jwtSecret string

@description('Google Sheet ID (placeholder).')
param googleSheetId string

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

var secrets = [
  {
    name: 'cosmos-connection-string'
    value: cosmosConnectionString
  }
  {
    name: 'blob-connection-string'
    value: blobConnectionString
  }
  {
    name: 'acs-connection-string'
    value: acsConnectionString
  }
  {
    name: 'jwt-secret'
    value: jwtSecret
  }
  {
    name: 'google-sheet-id'
    value: googleSheetId
  }
]

resource secretResources 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = [
  for s in secrets: {
    parent: keyVault
    name: s.name
    properties: {
      value: s.value
      contentType: 'text/plain'
    }
  }
]
