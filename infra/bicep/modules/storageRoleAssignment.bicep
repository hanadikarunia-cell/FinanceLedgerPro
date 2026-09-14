// =============================================================================
// Storage data-plane role assignment.
// Grants a principal the "Storage Blob Data Contributor" role so the API's
// managed identity can read/write blobs without account keys.
// =============================================================================
@description('Name of an existing Storage account.')
param storageAccountName string

@description('Object (principal) ID of the managed identity.')
param principalId string

// Built-in role: Storage Blob Data Contributor.
var blobDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, principalId, blobDataContributorRoleId)
  scope: storage
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', blobDataContributorRoleId)
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}
