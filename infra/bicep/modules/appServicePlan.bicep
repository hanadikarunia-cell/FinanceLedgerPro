// =============================================================================
// App Service Plan (Linux)
// =============================================================================
@description('Azure region.')
param location string

@description('Resource tags.')
param tags object = {}

@description('App Service Plan name.')
param planName string

@description('SKU name, e.g. B1, P0v3, P1v3.')
param sku string = 'P1v3'

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  tags: tags
  sku: {
    name: sku
  }
  kind: 'linux'
  properties: {
    reserved: true // required for Linux
  }
}

output planId string = plan.id
output planName string = plan.name
