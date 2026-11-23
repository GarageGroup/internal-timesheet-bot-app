targetScope = 'resourceGroup'

@description('Default Azure region used when the Function App is created.')
param location string = resourceGroup().location

@description('Resolved Function App region. Existing resource location is preserved by the installation script.')
param functionLocation string = location

param functionAppName string
param appServicePlanName string
param storageAccountName string
param applicationInsightsName string
param dataverseManagedIdentityName string
@description('Base64-encoded JSON tags. Existing Function App tags are preserved by the installation script.')
param functionTagsBase64 string

resource plan 'Microsoft.Web/serverfarms@2024-04-01' existing = {
  name: appServicePlanName
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

resource insights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: applicationInsightsName
}

resource dataverseIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: dataverseManagedIdentityName
}

resource agentIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: '${functionAppName}-agent'
}

resource functionApp 'Microsoft.Web/sites@2024-04-01' = {
  name: functionAppName
  location: functionLocation
  kind: 'functionapp,linux'
  tags: base64ToJson(functionTagsBase64)
  identity: {
    type: 'SystemAssigned, UserAssigned'
    userAssignedIdentities: {
      '${dataverseIdentity.id}': {}
      '${agentIdentity.id}': {}
    }
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    publicNetworkAccess: 'Enabled'
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|10.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
      use32BitWorkerProcess: false
    }
  }
}

output functionAppName string = functionApp.name
output functionAppHostName string = functionApp.properties.defaultHostName
output functionPrincipalId string = functionApp.identity.principalId
output dataverseIdentityClientId string = dataverseIdentity.properties.clientId
output agentIdentityClientId string = agentIdentity.properties.clientId
output storageAccountName string = storage.name
output applicationInsightsConnectionString string = insights.properties.ConnectionString
