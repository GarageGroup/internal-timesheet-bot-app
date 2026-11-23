#!/usr/bin/env bash
set -euo pipefail

required=(
  AZURE_RESOURCE_GROUP_NAME
  AZURE_SUBSCRIPTION_ID
  FUNCTION_APP_NAME
  STORAGE_ACCOUNT_NAME
  APPLICATION_INSIGHTS_NAME
  DATAVERSE_MANAGED_IDENTITY_CLIENT_ID
  AGENT_MANAGED_IDENTITY_CLIENT_ID
  API_WEB_APP_NAME
  APIM_SERVICE_NAME
  APIM_AGENT_API_PATH
  DATAVERSE_SERVICE_URL
  BOT_APIM_SERVICE_NAME
  BOT_WEB_APP_BASE_ADDRESS
  WELCOME_IMAGE_URL
  TIMESHEET_LIMITATION_DAY_OF_MONTH
)

for variable in "${required[@]}"; do
  [[ -n "${!variable:-}" ]] || { echo "Missing $variable" >&2; exit 1; }
done

storage_key="$(az storage account keys list \
  --resource-group "$AZURE_RESOURCE_GROUP_NAME" \
  --account-name "$STORAGE_ACCOUNT_NAME" \
  --query '[0].value' \
  --output tsv \
  --only-show-errors)"
[[ -n "$storage_key" ]] || { echo 'Storage account key was not found' >&2; exit 1; }

storage_connection="DefaultEndpointsProtocol=https;AccountName=$STORAGE_ACCOUNT_NAME;AccountKey=$storage_key;EndpointSuffix=core.windows.net"
insights_connection="$(az monitor app-insights component show \
  --resource-group "$AZURE_RESOURCE_GROUP_NAME" \
  --app "$APPLICATION_INSIGHTS_NAME" \
  --query connectionString \
  --output tsv \
  --only-show-errors)"
[[ -n "$insights_connection" ]] || { echo 'Application Insights connection string was not found' >&2; exit 1; }

agent_audience="$(az webapp config appsettings list \
  --resource-group "$AZURE_RESOURCE_GROUP_NAME" \
  --name "$API_WEB_APP_NAME" \
  --query "[?name=='Agent__Authentication__Audience'].value | [0]" \
  --output tsv \
  --only-show-errors)"
[[ -n "$agent_audience" ]] || { echo 'Agent API audience was not found in API Web App settings' >&2; exit 1; }
agent_api_base_address="https://${APIM_SERVICE_NAME}.azure-api.net/${APIM_AGENT_API_PATH#/}"
agent_api_base_address="${agent_api_base_address%/}/"

telegram_subscription_url="https://management.azure.com/subscriptions/$AZURE_SUBSCRIPTION_ID/resourceGroups/$AZURE_RESOURCE_GROUP_NAME/providers/Microsoft.ApiManagement/service/$BOT_APIM_SERVICE_NAME/subscriptions/TelegramApiSubscription/listSecrets?api-version=2024-05-01"
telegram_api_key="$(az rest --method POST --url "$telegram_subscription_url" --query primaryKey --output tsv --only-show-errors)"
[[ -n "$telegram_api_key" ]] || { echo 'Telegram APIM subscription key was not found' >&2; exit 1; }
telegram_base_address="https://${BOT_APIM_SERVICE_NAME}.azure-api.net/telegram/api/"

az functionapp config appsettings set \
  --resource-group "$AZURE_RESOURCE_GROUP_NAME" \
  --name "$FUNCTION_APP_NAME" \
  --settings \
    "FUNCTIONS_EXTENSION_VERSION=~4" \
    "FUNCTIONS_WORKER_RUNTIME=dotnet-isolated" \
    "WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED=1" \
    "SCM_DO_BUILD_DURING_DEPLOYMENT=false" \
    "AzureWebJobsStorage=$storage_connection" \
    "APPLICATIONINSIGHTS_CONNECTION_STRING=$insights_connection" \
    "AZURE_CLIENT_ID=$DATAVERSE_MANAGED_IDENTITY_CLIENT_ID" \
    "AgentApi__ManagedIdentityClientId=$AGENT_MANAGED_IDENTITY_CLIENT_ID" \
    "AgentApi__Audience=$agent_audience" \
    "AgentApi__BaseAddress=$agent_api_base_address" \
    "BlobBotStorage__AccountName=$STORAGE_ACCOUNT_NAME" \
    "BlobBotStorage__AccountKey=$storage_key" \
    "Dataverse__ServiceUrl=$DATAVERSE_SERVICE_URL" \
    "TelegramBot__ApiKey=$telegram_api_key" \
    "TelegramBot__BaseAddress=$telegram_base_address" \
    "Bot__FileUrlTemplate=${telegram_base_address}{0}" \
    "Bot__WebAppBaseAddress=$BOT_WEB_APP_BASE_ADDRESS" \
    "AgentVoice__MaxFileSizeBytes=5242880" \
    "Welcome__ImageUrl=$WELCOME_IMAGE_URL" \
    "TimesheetEdit__LimitationDayOfMonth=$TIMESHEET_LIMITATION_DAY_OF_MONTH" \
  --output none \
  --only-show-errors

echo "Function App settings configured for $FUNCTION_APP_NAME."
