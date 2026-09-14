// =============================================================================
// Grant a principal the "Key Vault Secrets User" role on a Key Vault.
// Used to let App Service managed identities read secrets via KV references.
// =============================================================================
@description('Name of an existing Key Vault.')
param keyVaultName string

@description('Object (principal) ID of the managed identity.')
param principalId string

// Built-in role: Key Vault Secrets User (read secret values).
var secretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, principalId, secretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', secretsUserRoleId)
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}
