targetScope = 'resourceGroup'

param apimServiceName string
param functionAppName string
@secure()
param telegramBotToken string
@secure()
param functionMasterKey string

resource apim 'Microsoft.ApiManagement/service@2024-05-01' existing = {
  name: apimServiceName
}

resource telegramToken 'Microsoft.ApiManagement/service/namedValues@2024-05-01' = {
  parent: apim
  name: 'TelegramBotApiKey'
  properties: {
    displayName: 'TelegramBotApiKey'
    secret: true
    value: telegramBotToken
  }
}

resource botFunctionCode 'Microsoft.ApiManagement/service/namedValues@2024-05-01' = {
  parent: apim
  name: 'BotFunctionCode'
  properties: {
    displayName: 'BotFunctionCode'
    secret: true
    value: functionMasterKey
  }
}

resource botApi 'Microsoft.ApiManagement/service/apis@2024-05-01' = {
  parent: apim
  name: 'gtimesheet-bot-api'
  properties: {
    displayName: 'GTimesheet Bot Api'
    path: 'bot'
    protocols: [
      'https'
    ]
    serviceUrl: 'https://${functionAppName}.azurewebsites.net/'
    subscriptionRequired: true
    subscriptionKeyParameterNames: {
      header: 'Ocp-Apim-Subscription-Key'
      query: 'subscription-key'
    }
  }
}

resource botApiPolicy 'Microsoft.ApiManagement/service/apis/policies@2024-05-01' = {
  parent: botApi
  name: 'policy'
  properties: {
    format: 'rawxml'
    value: '<policies><inbound><base /><set-query-parameter name="code" exists-action="override"><value>{{BotFunctionCode}}</value></set-query-parameter></inbound><backend><base /></backend><outbound><base /></outbound><on-error><base /></on-error></policies>'
  }
  dependsOn: [
    botFunctionCode
  ]
}

resource botMessage 'Microsoft.ApiManagement/service/apis/operations@2024-05-01' = {
  parent: botApi
  name: 'post-bot-message'
  properties: {
    displayName: 'Post Bot Message'
    method: 'POST'
    urlTemplate: '/message'
    templateParameters: []
    responses: []
  }
}

resource botHealth 'Microsoft.ApiManagement/service/apis/operations@2024-05-01' = {
  parent: botApi
  name: 'health-check'
  properties: {
    displayName: 'Health Check'
    method: 'GET'
    urlTemplate: '/health'
    templateParameters: []
    responses: []
  }
}

// Microsoft Entra ID posts the authorization code of a user sign-in to this route
resource oauthApi 'Microsoft.ApiManagement/service/apis@2024-05-01' = {
  parent: apim
  name: 'gtimesheet-oauth2-0-api'
  properties: {
    displayName: 'GTimesheet OAuth2.0 API'
    path: 'oauth2'
    protocols: [
      'https'
    ]
    serviceUrl: 'https://${functionAppName}.azurewebsites.net/'
    subscriptionRequired: false
  }
}

resource oauthApiPolicy 'Microsoft.ApiManagement/service/apis/policies@2024-05-01' = {
  parent: oauthApi
  name: 'policy'
  properties: {
    format: 'rawxml'
    value: '<policies><inbound><base /><set-query-parameter name="code" exists-action="override"><value>{{BotFunctionCode}}</value></set-query-parameter></inbound><backend><base /></backend><outbound><base /></outbound><on-error><base /></on-error></policies>'
  }
  dependsOn: [
    botFunctionCode
  ]
}

resource oauthCallback 'Microsoft.ApiManagement/service/apis/operations@2024-05-01' = {
  parent: oauthApi
  name: 'authorize-user'
  properties: {
    displayName: 'Authorize User'
    method: 'POST'
    urlTemplate: '/callback'
    templateParameters: []
    responses: []
  }
}

resource oauthCallbackPolicy 'Microsoft.ApiManagement/service/apis/operations/policies@2024-05-01' = {
  parent: oauthCallback
  name: 'policy'
  properties: {
    format: 'rawxml'
    value: '<policies><inbound><base /><rewrite-uri template="/oauth2/callback" /></inbound><backend><base /></backend><outbound><base /></outbound><on-error><base /></on-error></policies>'
  }
}

resource telegramApi 'Microsoft.ApiManagement/service/apis@2024-05-01' = {
  parent: apim
  name: 'gtimesheet-telegram-api'
  properties: {
    displayName: 'GTimesheet Telegram API'
    path: 'telegram/api'
    protocols: [
      'https'
    ]
    serviceUrl: 'https://api.telegram.org/'
    subscriptionRequired: true
    subscriptionKeyParameterNames: {
      header: 'Ocp-Apim-Subscription-Key'
      query: 'subscription-key'
    }
  }
}

resource telegramSend 'Microsoft.ApiManagement/service/apis/operations@2024-05-01' = {
  parent: telegramApi
  name: 'send-request'
  properties: {
    displayName: 'Send Request'
    method: 'POST'
    urlTemplate: '/{methodName}'
    templateParameters: [
      {
        name: 'methodName'
        type: 'string'
        required: true
      }
    ]
    responses: []
  }
}

resource telegramSendPolicy 'Microsoft.ApiManagement/service/apis/operations/policies@2024-05-01' = {
  parent: telegramSend
  name: 'policy'
  properties: {
    format: 'rawxml'
    value: '<policies><inbound><base /><rewrite-uri template="@{ return &quot;/bot{{TelegramBotApiKey}}/&quot; + context.Request.MatchedParameters[&quot;methodName&quot;]; }" /></inbound><backend><base /></backend><outbound><base /></outbound><on-error><base /></on-error></policies>'
  }
  dependsOn: [
    telegramToken
  ]
}

resource telegramGetFile 'Microsoft.ApiManagement/service/apis/operations@2024-05-01' = {
  parent: telegramApi
  name: 'get-file'
  properties: {
    displayName: 'Get File'
    method: 'GET'
    urlTemplate: '/{type}/{name}'
    templateParameters: [
      {
        name: 'type'
        type: 'string'
        required: true
      }
      {
        name: 'name'
        type: 'string'
        required: true
      }
    ]
    responses: []
  }
}

resource telegramGetFilePolicy 'Microsoft.ApiManagement/service/apis/operations/policies@2024-05-01' = {
  parent: telegramGetFile
  name: 'policy'
  properties: {
    format: 'rawxml'
    value: '<policies><inbound><base /><rewrite-uri template="@{ return &quot;/file/bot{{TelegramBotApiKey}}/&quot; + context.Request.MatchedParameters[&quot;type&quot;] + &quot;/&quot; + context.Request.MatchedParameters[&quot;name&quot;]; }" /></inbound><backend><base /></backend><outbound><base /></outbound><on-error><base /></on-error></policies>'
  }
  dependsOn: [
    telegramToken
  ]
}
