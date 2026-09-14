// =============================================================================
// API App Service - ASP.NET Core 8 (Linux)
// - System-assigned managed identity
// - HTTPS only, Always On
// - App settings pull secrets via Key Vault references
// =============================================================================
@description('Azure region.')
param location string

@description('Resource tags.')
param tags object = {}

@description('App Service (site) name.')
param appServiceName string

@description('Resource ID of the App Service Plan.')
param appServicePlanId string

@description('Key Vault name used for @Microsoft.KeyVault references.')
param keyVaultName string

@description('Application Insights connection string.')
param appInsightsConnectionString string

@description('Cosmos DB endpoint (for identity-based access).')
param cosmosEndpoint string

@description('Storage account name (for identity-based access).')
param storageAccountName string

@description('Azure Communication Services endpoint / host name.')
param acsEndpoint string

@description('.NET runtime version for Linux.')
param netVersion string = 'DOTNETCORE|8.0'

// Key Vault reference helper: resolves secret at runtime via managed identity.
func kvRef(vaultName string, secretName string) string =>
  '@Microsoft.KeyVault(SecretUri=https://${vaultName}${environment().suffixes.keyvaultDns}/secrets/${secretName})'

resource api 'Microsoft.Web/sites@2023-12-01' = {
  name: appServiceName
  location: location
  tags: tags
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: netVersion
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
      healthCheckPath: '/health'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        // ----- Cosmos: identity-based endpoint + KV connection string fallback -----
        {
          name: 'Cosmos__Endpoint'
          value: cosmosEndpoint
        }
        {
          name: 'Cosmos__DatabaseName'
          value: 'FinanceLedger'
        }
        {
          name: 'ConnectionStrings__Cosmos'
          value: kvRef(keyVaultName, 'cosmos-connection-string')
        }
        // ----- Blob storage -----
        {
          name: 'Storage__AccountName'
          value: storageAccountName
        }
        {
          name: 'Storage__ContainerName'
          value: 'attachments'
        }
        {
          name: 'ConnectionStrings__Blob'
          value: kvRef(keyVaultName, 'blob-connection-string')
        }
        // ----- Auth -----
        {
          name: 'Jwt__Secret'
          value: kvRef(keyVaultName, 'jwt-secret')
        }
        // ----- Azure Communication Services (email) -----
        {
          name: 'Acs__Endpoint'
          value: acsEndpoint
        }
        {
          name: 'ConnectionStrings__Acs'
          value: kvRef(keyVaultName, 'acs-connection-string')
        }
        // ----- Google Sheets sync -----
        {
          name: 'GoogleSheets__SheetId'
          value: kvRef(keyVaultName, 'google-sheet-id')
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
      ]
    }
  }
}

output principalId string = api.identity.principalId
output defaultHostName string = api.properties.defaultHostName
output appServiceName string = api.name
