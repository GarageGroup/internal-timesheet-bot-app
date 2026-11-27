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
  APIM_SERVICE_NAME
  DATAVERSE_SERVICE_URL
  BOT_APIM_SERVICE_NAME
  BOT_WEB_APP_BASE_ADDRESS
  WELCOME_IMAGE_URL
  TIMESHEET_LIMITATION_DAY_OF_MONTH
  FOUNDRY_PROJECT_ENDPOINT
  FOUNDRY_OPENAI_ENDPOINT
  FOUNDRY_CHAT_DEPLOYMENT_NAME
  FOUNDRY_VOICE_DEPLOYMENT_NAME
  AGENT_SIGN_IN_CLIENT_ID
  AGENT_TOKEN_ENCRYPTION_KEY
  TIMESHEET_API_AUDIENCE
  TIMESHEET_API_PATH
  TIMESHEET_SWAGGER_SUBSCRIPTION_KEY
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

# The agent: the chat model, user sign-in and the Timesheet API as an agent tool.
sign_in_tenant_id="$(az account show --query tenantId --output tsv --only-show-errors)"
shared_apim_address="https://${APIM_SERVICE_NAME}.azure-api.net"
timesheet_api_path="${TIMESHEET_API_PATH#/}"

agent_settings=(
  "Agent__Foundry__ProjectEndpoint=$FOUNDRY_PROJECT_ENDPOINT"
  "Agent__Foundry__ModelId=$FOUNDRY_CHAT_DEPLOYMENT_NAME"
  "Agent__Foundry__ManagedIdentityClientId=$AGENT_MANAGED_IDENTITY_CLIENT_ID"
  "Agent__Voice__Endpoint=$FOUNDRY_OPENAI_ENDPOINT"
  "Agent__Voice__DeploymentName=$FOUNDRY_VOICE_DEPLOYMENT_NAME"
  "Agent__Voice__ModelId=${FOUNDRY_VOICE_MODEL_NAME:-$FOUNDRY_VOICE_DEPLOYMENT_NAME}"
  "Agent__SignIn__TenantId=$sign_in_tenant_id"
  "Agent__SignIn__ClientId=$AGENT_SIGN_IN_CLIENT_ID"
  "Agent__SignIn__RedirectUri=https://${BOT_APIM_SERVICE_NAME}.azure-api.net/oauth2/callback"
  "Agent__SignIn__TokenEncryptionKey=$AGENT_TOKEN_ENCRYPTION_KEY"
  'Agent__Tools__0__Name=Timesheet'
  'Agent__Tools__0__Type=OpenApi'
  "Agent__Tools__0__Url=${shared_apim_address}/swagger/${TIMESHEET_SWAGGER_NAME:-timesheet}/swagger.json"
  "Agent__Tools__0__BaseAddress=${shared_apim_address}/${timesheet_api_path%/}/"
  "Agent__Tools__0__Audience=$TIMESHEET_API_AUDIENCE"
  "Agent__Tools__0__SpecificationSubscriptionKey=$TIMESHEET_SWAGGER_SUBSCRIPTION_KEY"
)

read -r -a allowed_operations <<< "${TIMESHEET_ALLOWED_OPERATIONS:-get-periods get-tags get-timesheets get-last-projects search-projects create-timesheet update-timesheet delete-timesheet}"
for index in "${!allowed_operations[@]}"; do
  agent_settings+=("Agent__Tools__0__AllowedOperations__${index}=${allowed_operations[$index]}")
done

# These operations change data, so the bot executes them only after the user presses the Confirm button.
read -r -a confirmation_operations <<< "${TIMESHEET_CONFIRMATION_OPERATIONS:-create-timesheet update-timesheet delete-timesheet}"
for index in "${!confirmation_operations[@]}"; do
  agent_settings+=("Agent__Tools__0__ConfirmationOperations__${index}=${confirmation_operations[$index]}")
done

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
    "${agent_settings[@]}" \
  --output none \
  --only-show-errors

# Settings of the removed Timesheet agent API are deleted, so they do not look like they are still in use.
az functionapp config appsettings delete \
  --resource-group "$AZURE_RESOURCE_GROUP_NAME" \
  --name "$FUNCTION_APP_NAME" \
  --setting-names Agent__Enabled AgentApi__ManagedIdentityClientId AgentApi__Audience AgentApi__BaseAddress Agent__SignIn__ManagedIdentityClientId \
  --output none \
  --only-show-errors

echo "Function App settings configured for $FUNCTION_APP_NAME."
