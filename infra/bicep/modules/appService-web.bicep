// =============================================================================
// Web App Service - React build served on Linux (Node runtime)
// Serves the static React production build. For a pure static site consider
// Azure Static Web Apps; App Service is used here to keep parity with the API.
// =============================================================================
@description('Azure region.')
param location string

@description('Resource tags.')
param tags object = {}

@description('App Service (site) name.')
param appServiceName string

@description('Resource ID of the App Service Plan.')
param appServicePlanId string

@description('Application Insights connection string.')
param appInsightsConnectionString string

@description('Base URL of the API the web app calls.')
param apiBaseUrl string

@description('Node runtime version for Linux.')
param nodeVersion string = 'NODE|20-lts'

@description('Startup command to serve the built React app (e.g. via "serve" or pm2).')
param startupCommand string = 'pm2 serve /home/site/wwwroot --no-daemon --spa'

resource web 'Microsoft.Web/sites@2023-12-01' = {
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
      linuxFxVersion: nodeVersion
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
      appCommandLine: startupCommand
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'WEBSITE_NODE_DEFAULT_VERSION'
          value: '~20'
        }
        // Baked-in for reference; CRA/Vite typically inline env at build time.
        {
          name: 'REACT_APP_API_BASE_URL'
          value: apiBaseUrl
        }
        {
          name: 'SCM_DO_BUILD_DURING_DEPLOYMENT'
          value: 'false'
        }
      ]
    }
  }
}

output principalId string = web.identity.principalId
output defaultHostName string = web.properties.defaultHostName
output appServiceName string = web.name
