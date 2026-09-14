// =============================================================================
// Azure Cosmos DB - SQL (Core) API
// Database: FinanceLedger
// Containers: Users(/id), Transactions(/branch), AuditLogs(/userId),
//             Branches(/id), Attachments(/transactionId)
// Cost: serverless (default) or autoscale.
// =============================================================================
@description('Azure region.')
param location string

@description('Resource tags.')
param tags object = {}

@description('Cosmos DB account name (globally unique, lowercase).')
param cosmosAccountName string

@description('SQL database name.')
param databaseName string = 'FinanceLedger'

@description('Capacity mode.')
@allowed([
  'serverless'
  'autoscale'
])
param capacityMode string = 'serverless'

@description('Autoscale max RU/s per container (used only when capacityMode = autoscale).')
param autoscaleMaxThroughput int = 4000

var enableServerless = capacityMode == 'serverless'

// Standard indexing policy: index everything, exclude the _etag system path.
var defaultIndexingPolicy = {
  indexingMode: 'consistent'
  automatic: true
  includedPaths: [
    {
      path: '/*'
    }
  ]
  excludedPaths: [
    {
      path: '/"_etag"/?'
    }
  ]
}

// Per-container throughput options; empty object for serverless.
var throughputOptions = enableServerless
  ? {}
  : {
      autoscaleSettings: {
        maxThroughput: autoscaleMaxThroughput
      }
    }

var containers = [
  {
    name: 'Users'
    partitionKey: '/id'
  }
  {
    name: 'Transactions'
    partitionKey: '/branch'
  }
  {
    name: 'AuditLogs'
    partitionKey: '/userId'
  }
  {
    name: 'Branches'
    partitionKey: '/id'
  }
  {
    name: 'Attachments'
    partitionKey: '/transactionId'
  }
]

resource account 'Microsoft.DocumentDB/databaseAccounts@2024-11-15' = {
  name: cosmosAccountName
  location: location
  tags: tags
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    enableAutomaticFailover: !enableServerless
    disableLocalAuth: false
    minimalTlsVersion: 'Tls12'
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    capabilities: enableServerless
      ? [
          {
            name: 'EnableServerless'
          }
        ]
      : []
    backupPolicy: {
      type: 'Continuous'
      continuousModeProperties: {
        tier: 'Continuous7Days'
      }
    }
  }
}

resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-11-15' = {
  parent: account
  name: databaseName
  properties: {
    resource: {
      id: databaseName
    }
  }
}

resource containerResources 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-11-15' = [
  for c in containers: {
    parent: database
    name: c.name
    properties: {
      resource: {
        id: c.name
        partitionKey: {
          paths: [
            c.partitionKey
          ]
          kind: 'Hash'
          version: 2
        }
        indexingPolicy: defaultIndexingPolicy
      }
      options: throughputOptions
    }
  }
]

output accountName string = account.name
output endpoint string = account.properties.documentEndpoint
#disable-next-line outputs-should-not-contain-secrets
output connectionString string = account.listConnectionStrings().connectionStrings[0].connectionString
