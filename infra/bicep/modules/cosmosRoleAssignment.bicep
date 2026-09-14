// =============================================================================
// Cosmos DB SQL data-plane role assignment.
// Grants a principal the built-in "Cosmos DB Built-in Data Contributor" role
// so the API's managed identity can read/write documents without keys.
// =============================================================================
@description('Name of an existing Cosmos DB account.')
param cosmosAccountName string

@description('Object (principal) ID of the managed identity.')
param principalId string

// Built-in Cosmos DB data-plane role: Data Contributor (id suffix 00000000-0000-0000-0000-000000000002).
resource account 'Microsoft.DocumentDB/databaseAccounts@2024-11-15' existing = {
  name: cosmosAccountName
}

var dataContributorRoleId = '/${subscription().id}/resourceGroups/${resourceGroup().name}/providers/Microsoft.DocumentDB/databaseAccounts/${cosmosAccountName}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002'

resource sqlRoleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-11-15' = {
  parent: account
  name: guid(account.id, principalId, '00000000-0000-0000-0000-000000000002')
  properties: {
    roleDefinitionId: dataContributorRoleId
    principalId: principalId
    scope: account.id
  }
}
