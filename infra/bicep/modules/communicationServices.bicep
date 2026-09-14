// =============================================================================
// Azure Communication Services + Email
// - Communication Services resource
// - Email Communication Service
// - Azure-managed email domain (free "AzureManagedDomain")
// - Links the managed domain to the Communication Services resource
//
// NOTE: The Azure-managed domain lets you send from a
//   DoNotReply@<random-guid>.azurecomm.net address with no DNS setup, ideal
//   for dev/test and low-volume notifications. For production branded senders,
//   add a CustomDomains sub-resource and verify DNS TXT/SPF/DKIM records.
// ACS + Email are global services; only dataLocation controls data residency.
// =============================================================================
@description('Resource tags.')
param tags object = {}

@description('Communication Services resource name.')
param acsName string

@description('Email Communication Service resource name.')
param emailServiceName string

@description('Data residency location for ACS/Email (e.g. "United States", "Europe").')
param dataLocation string = 'United States'

resource emailService 'Microsoft.Communication/emailServices@2023-06-01-preview' = {
  name: emailServiceName
  location: 'global'
  tags: tags
  properties: {
    dataLocation: dataLocation
  }
}

// Free Azure-managed domain (no DNS verification required).
resource managedDomain 'Microsoft.Communication/emailServices/domains@2023-06-01-preview' = {
  parent: emailService
  name: 'AzureManagedDomain'
  location: 'global'
  tags: tags
  properties: {
    domainManagement: 'AzureManaged'
    userEngagementTracking: 'Disabled'
  }
}

resource acs 'Microsoft.Communication/communicationServices@2023-06-01-preview' = {
  name: acsName
  location: 'global'
  tags: tags
  properties: {
    dataLocation: dataLocation
    // Link the email domain so ACS can send email from it.
    linkedDomains: [
      managedDomain.id
    ]
  }
}

output acsName string = acs.name
output hostName string = acs.properties.hostName
output emailFromDomain string = managedDomain.properties.fromSenderDomain
#disable-next-line outputs-should-not-contain-secrets
output connectionString string = acs.listKeys().primaryConnectionString
