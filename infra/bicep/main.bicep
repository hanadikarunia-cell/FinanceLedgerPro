// =============================================================================
// Finance Ledger Pro - Main orchestration template
// Scope: Resource Group (deploy with `az deployment group create`)
// =============================================================================
targetScope = 'resourceGroup'

// -----------------------------------------------------------------------------
// Parameters
// -----------------------------------------------------------------------------
@description('Short environment name, e.g. dev / prod.')
@allowed([
  'dev'
  'test'
  'prod'
])
param env string = 'dev'

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Base application name used to derive resource names.')
@minLength(3)
@maxLength(18)
param appName string = 'financeledger'

@description('App Service Plan SKU (e.g. B1, P0v3, P1v3).')
param appServicePlanSku string = 'P1v3'

@description('Cosmos DB capacity mode.')
@allowed([
  'serverless'
  'autoscale'
])
param cosmosCapacityMode string = 'serverless'

@description('Max autoscale RU/s per container (only used when cosmosCapacityMode = autoscale).')
param cosmosAutoscaleMaxThroughput int = 4000

@description('Object ID (principal) that should be granted admin access to Key Vault secrets, e.g. a deployment SP or your user. Leave empty to skip.')
param adminObjectId string = ''

@description('Tags applied to all resources.')
param tags object = {
  application: 'FinanceLedgerPro'
  environment: env
  managedBy: 'bicep'
}

// -----------------------------------------------------------------------------
// Naming helpers
// -----------------------------------------------------------------------------
var suffix = uniqueString(resourceGroup().id, appName, env)
var namePrefix = '${appName}-${env}'

// Storage account names: 3-24 chars, lowercase alphanumeric only.
var storageAccountName = toLower(take(replace('${appName}${env}${suffix}', '-', ''), 24))
// Key Vault names: 3-24 chars, alphanumeric + hyphen.
var keyVaultName = take('${appName}-${env}-kv-${take(suffix, 6)}', 24)
var cosmosAccountName = toLower(take('${appName}-${env}-cosmos-${take(suffix, 6)}', 44))

// -----------------------------------------------------------------------------
// Observability (Log Analytics + Application Insights)
// -----------------------------------------------------------------------------
module appInsights 'modules/appInsights.bicep' = {
  name: 'appInsights'
  params: {
    location: location
    tags: tags
    workspaceName: '${namePrefix}-law'
    appInsightsName: '${namePrefix}-ai'
  }
}

// -----------------------------------------------------------------------------
// Key Vault
// -----------------------------------------------------------------------------
module keyVault 'modules/keyVault.bicep' = {
  name: 'keyVault'
  params: {
    location: location
    tags: tags
    keyVaultName: keyVaultName
    adminObjectId: adminObjectId
    tenantId: subscription().tenantId
  }
}

// -----------------------------------------------------------------------------
// Storage (Blob) - attachments
// -----------------------------------------------------------------------------
module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: {
    location: location
    tags: tags
    storageAccountName: storageAccountName
    containerName: 'attachments'
  }
}

// -----------------------------------------------------------------------------
// Cosmos DB (SQL / Core API)
// -----------------------------------------------------------------------------
module cosmos 'modules/cosmos.bicep' = {
  name: 'cosmos'
  params: {
    location: location
    tags: tags
    cosmosAccountName: cosmosAccountName
    databaseName: 'FinanceLedger'
    capacityMode: cosmosCapacityMode
    autoscaleMaxThroughput: cosmosAutoscaleMaxThroughput
  }
}

// -----------------------------------------------------------------------------
// Azure Communication Services + Email
// -----------------------------------------------------------------------------
module communicationServices 'modules/communicationServices.bicep' = {
  name: 'communicationServices'
  params: {
    tags: tags
    acsName: '${namePrefix}-acs'
    emailServiceName: '${namePrefix}-email'
    dataLocation: 'United States'
  }
}

// -----------------------------------------------------------------------------
// App Service Plan (Linux)
// -----------------------------------------------------------------------------
module appServicePlan 'modules/appServicePlan.bicep' = {
  name: 'appServicePlan'
  params: {
    location: location
    tags: tags
    planName: '${namePrefix}-plan'
    sku: appServicePlanSku
  }
}

// -----------------------------------------------------------------------------
// Secrets written into Key Vault (values are placeholders / references).
// In production these should be set out-of-band or via secure pipeline vars.
// -----------------------------------------------------------------------------
module secrets 'modules/keyVaultSecrets.bicep' = {
  name: 'keyVaultSecrets'
  params: {
    keyVaultName: keyVault.outputs.keyVaultName
    cosmosConnectionString: cosmos.outputs.connectionString
    blobConnectionString: storage.outputs.connectionString
    acsConnectionString: communicationServices.outputs.connectionString
    // jwt-secret and google-sheet-id are placeholders; rotate/replace after deploy.
    jwtSecret: 'REPLACE_ME_WITH_A_STRONG_SECRET'
    googleSheetId: 'REPLACE_ME_WITH_GOOGLE_SHEET_ID'
  }
}

// -----------------------------------------------------------------------------
// API App Service (.NET 8, Linux)
// -----------------------------------------------------------------------------
module apiApp 'modules/appService-api.bicep' = {
  name: 'apiApp'
  params: {
    location: location
    tags: tags
    appServiceName: '${namePrefix}-api'
    appServicePlanId: appServicePlan.outputs.planId
    keyVaultName: keyVault.outputs.keyVaultName
    appInsightsConnectionString: appInsights.outputs.connectionString
    cosmosEndpoint: cosmos.outputs.endpoint
    storageAccountName: storage.outputs.storageAccountName
    acsEndpoint: communicationServices.outputs.hostName
  }
}

// -----------------------------------------------------------------------------
// Web App Service (React build served on Linux)
// -----------------------------------------------------------------------------
module webApp 'modules/appService-web.bicep' = {
  name: 'webApp'
  params: {
    location: location
    tags: tags
    appServiceName: '${namePrefix}-web'
    appServicePlanId: appServicePlan.outputs.planId
    appInsightsConnectionString: appInsights.outputs.connectionString
    apiBaseUrl: 'https://${apiApp.outputs.defaultHostName}'
  }
}

// -----------------------------------------------------------------------------
// Grant App Service managed identities access to Key Vault secrets (RBAC).
// -----------------------------------------------------------------------------
module apiKvAccess 'modules/keyVaultRoleAssignment.bicep' = {
  name: 'apiKvAccess'
  params: {
    keyVaultName: keyVault.outputs.keyVaultName
    principalId: apiApp.outputs.principalId
  }
}

module webKvAccess 'modules/keyVaultRoleAssignment.bicep' = {
  name: 'webKvAccess'
  params: {
    keyVaultName: keyVault.outputs.keyVaultName
    principalId: webApp.outputs.principalId
  }
}

// -----------------------------------------------------------------------------
// Grant API managed identity data-plane access to Cosmos + Storage.
// -----------------------------------------------------------------------------
module apiCosmosAccess 'modules/cosmosRoleAssignment.bicep' = {
  name: 'apiCosmosAccess'
  params: {
    cosmosAccountName: cosmos.outputs.accountName
    principalId: apiApp.outputs.principalId
  }
}

module apiStorageAccess 'modules/storageRoleAssignment.bicep' = {
  name: 'apiStorageAccess'
  params: {
    storageAccountName: storage.outputs.storageAccountName
    principalId: apiApp.outputs.principalId
  }
}

// -----------------------------------------------------------------------------
// Outputs
// -----------------------------------------------------------------------------
output apiUrl string = 'https://${apiApp.outputs.defaultHostName}'
output webUrl string = 'https://${webApp.outputs.defaultHostName}'
output keyVaultName string = keyVault.outputs.keyVaultName
output cosmosEndpoint string = cosmos.outputs.endpoint
output appInsightsConnectionString string = appInsights.outputs.connectionString
output storageAccountName string = storage.outputs.storageAccountName
