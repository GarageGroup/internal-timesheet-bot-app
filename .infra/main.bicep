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

@description('Resolved region of the agent identity. Existing identity location is preserved by the installation script.')
param agentIdentityLocation string = location

param environmentName string
param foundryAccountName string
param foundryProjectName string

@description('Resolved Foundry region. Existing account location is preserved by the installation script.')
param foundryLocation string = location
param foundrySubscriptionId string = subscription().subscriptionId
param foundryResourceGroupName string = resourceGroup().name

@description('False when the Foundry account belongs to another Resource Group: it is used as is and is not deployed here.')
param deployFoundry bool = true

@description('Base64-encoded JSON tags. Existing Foundry account tags are preserved by the installation script.')
param foundryTagsBase64 string

param chatDeploymentName string = 'gpt-5-mini'
param chatModelName string = 'gpt-5-mini'
param chatModelVersion string = '2025-08-07'
param chatDeploymentSkuName string = 'GlobalStandard'
param chatDeploymentCapacity int = 500

param voiceDeploymentName string = 'whisper'
param voiceModelName string = 'whisper'
param voiceModelVersion string = '001'
param voiceDeploymentSkuName string = 'Standard'
param voiceDeploymentCapacity int = 3

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

// The identity calls the chat model and is the federated credential of the user sign-in App Registration.
// It must never be recreated: its principal ID is registered in Microsoft Entra ID and in role assignments
resource agentIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${functionAppName}-agent'
  location: agentIdentityLocation
}

module foundryModule './foundry.bicep' = if (deployFoundry) {
  name: 'timesheet-bot-foundry-${environmentName}'
  scope: resourceGroup(foundrySubscriptionId, foundryResourceGroupName)
  params: {
    foundryLocation: foundryLocation
    foundryAccountName: foundryAccountName
    foundryProjectName: foundryProjectName
    applicationName: 'garage-timesheet'
    environmentName: environmentName
    tags: base64ToJson(foundryTagsBase64)
    chatDeploymentName: chatDeploymentName
    chatModelName: chatModelName
    chatModelVersion: chatModelVersion
    chatDeploymentSkuName: chatDeploymentSkuName
    chatDeploymentCapacity: chatDeploymentCapacity
    voiceDeploymentName: voiceDeploymentName
    voiceModelName: voiceModelName
    voiceModelVersion: voiceModelVersion
    voiceDeploymentSkuName: voiceDeploymentSkuName
    voiceDeploymentCapacity: voiceDeploymentCapacity
  }
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
output agentIdentityPrincipalId string = agentIdentity.properties.principalId
output foundryAccountId string = resourceId(foundrySubscriptionId, foundryResourceGroupName, 'Microsoft.CognitiveServices/accounts', foundryAccountName)
output foundryProjectEndpoint string = 'https://${foundryAccountName}.services.ai.azure.com/api/projects/${foundryProjectName}'
output foundryOpenAiEndpoint string = 'https://${foundryAccountName}.openai.azure.com/'
output chatDeploymentName string = chatDeploymentName
output voiceDeploymentName string = voiceDeploymentName
output voiceModelName string = voiceModelName
output storageAccountName string = storage.name
output applicationInsightsConnectionString string = insights.properties.ConnectionString
