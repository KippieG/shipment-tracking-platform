@description('Azure region for all resources.')
param location string = resourceGroup().location
@minLength(3)
param namePrefix string = 'shipment${uniqueString(resourceGroup().id)}'
@secure()
param sqlAdministratorPassword string
@minLength(1)
param sqlAdministratorLogin string = 'shipmentadmin'
@description('Creates Azure Service Bus only when asynchronous Azure messaging is required.')
param enableServiceBus bool = false

var tags = { application: 'shipment-tracking-platform', managedBy: 'bicep' }

resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${namePrefix}-logs'
  location: location
  tags: tags
  properties: { sku: { name: 'PerGB2018' }, retentionInDays: 30 }
}

resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${namePrefix}-insights'
  location: location
  kind: 'web'
  tags: tags
  properties: { Application_Type: 'web', WorkspaceResourceId: logs.id }
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: '${namePrefix}-kv'
  location: location
  tags: tags
  properties: {
    tenantId: subscription().tenantId
    sku: { family: 'A', name: 'standard' }
    enableRbacAuthorization: true
    publicNetworkAccess: 'Enabled'
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: replace('${namePrefix}store', '-', '')
  location: location
  tags: tags
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: { minimumTlsVersion: 'TLS1_2', allowBlobPublicAccess: false, supportsHttpsTrafficOnly: true }
}

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: '${namePrefix}-sql'
  location: location
  tags: tags
  properties: {
    administratorLogin: sqlAdministratorLogin
    administratorLoginPassword: sqlAdministratorPassword
    publicNetworkAccess: 'Enabled'
    minimalTlsVersion: '1.2'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: 'ShipmentDb'
  location: location
  sku: { name: 'Basic', tier: 'Basic' }
  properties: { collation: 'SQL_Latin1_General_CP1_CI_AS' }
}

resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = if (enableServiceBus) {
  name: '${namePrefix}-sb'
  location: location
  tags: tags
  sku: { name: 'Basic', tier: 'Basic' }
  properties: { minimumTlsVersion: '1.2' }
}

resource shipmentQueue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = if (enableServiceBus) {
  parent: serviceBus
  name: 'shipment-events'
  properties: { lockDuration: 'PT1M', maxDeliveryCount: 10, deadLetteringOnMessageExpiration: true }
}

resource environment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: '${namePrefix}-env'
  location: location
  tags: tags
  properties: { appLogsConfiguration: { destination: 'log-analytics', logAnalyticsConfiguration: { customerId: logs.properties.customerId, sharedKey: logs.listKeys().primarySharedKey } } }
}

resource app 'Microsoft.App/containerApps@2024-03-01' = {
  name: '${namePrefix}-api'
  location: location
  tags: tags
  identity: { type: 'SystemAssigned' }
  properties: {
    managedEnvironmentId: environment.id
    configuration: { ingress: { external: true, targetPort: 8080, transport: 'auto' }, activeRevisionsMode: 'Single' }
    template: {
      containers: [{ name: 'api', image: 'mcr.microsoft.com/dotnet/aspnet:10.0', resources: { cpu: json('0.25'), memory: '0.5Gi' }, env: [
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: insights.properties.ConnectionString }
        { name: 'KeyVault__Uri', value: keyVault.properties.vaultUri }
      ] }]
      scale: { minReplicas: 0, maxReplicas: 1 }
    }
  }
}

output containerAppName string = app.name
output containerAppFqdn string = app.properties.configuration.ingress.fqdn
output keyVaultUri string = keyVault.properties.vaultUri
output applicationInsightsConnectionString string = insights.properties.ConnectionString
output sqlServerName string = sqlServer.name
