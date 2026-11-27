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

# The in-bot agent: the chat model, user sign-in and the Timesheet API as an agent tool.
agent_enabled="${AGENT_ENABLED:-false}"
agent_settings=("Agent__Enabled=$agent_enabled")

if [[ "$agent_enabled" == 'true' ]]; then
  agent_required=(
    AGENT_SIGN_IN_CLIENT_ID
    AGENT_TOKEN_ENCRYPTION_KEY
    TIMESHEET_API_AUDIENCE
    TIMESHEET_API_PATH
    TIMESHEET_SWAGGER_SUBSCRIPTION_KEY
  )
  for variable in "${agent_required[@]}"; do
    [[ -n "${!variable:-}" ]] || { echo "Missing $variable: it is required when AGENT_ENABLED is true" >&2; exit 1; }
  done

  api_setting() {
    az webapp config appsettings list \
      --resource-group "$AZURE_RESOURCE_GROUP_NAME" \
      --name "$API_WEB_APP_NAME" \
      --query "[?name=='$1'].value | [0]" \
      --output tsv \
      --only-show-errors
  }

  # The bot uses the same Azure AI Foundry project and chat model as the Timesheet API.
  foundry_endpoint="$(api_setting Agent__Foundry__ProjectEndpoint)"
  foundry_model="$(api_setting Agent__Foundry__ModelId)"
  [[ -n "$foundry_endpoint" && -n "$foundry_model" ]] || { echo 'Foundry settings were not found in API Web App settings' >&2; exit 1; }

  sign_in_tenant_id="$(az account show --query tenantId --output tsv --only-show-errors)"
  shared_apim_address="https://${APIM_SERVICE_NAME}.azure-api.net"
  timesheet_api_path="${TIMESHEET_API_PATH#/}"

  agent_settings+=(
    "Agent__Foundry__ProjectEndpoint=$foundry_endpoint"
    "Agent__Foundry__ModelId=$foundry_model"
    "Agent__Foundry__ManagedIdentityClientId=$AGENT_MANAGED_IDENTITY_CLIENT_ID"
    "Agent__SignIn__TenantId=$sign_in_tenant_id"
    "Agent__SignIn__ClientId=$AGENT_SIGN_IN_CLIENT_ID"
    "Agent__SignIn__RedirectUri=https://${BOT_APIM_SERVICE_NAME}.azure-api.net/oauth2/callback"
    "Agent__SignIn__TokenEncryptionKey=$AGENT_TOKEN_ENCRYPTION_KEY"
    "Agent__SignIn__ManagedIdentityClientId=$AGENT_MANAGED_IDENTITY_CLIENT_ID"
    'Agent__Tools__0__Name=Timesheet'
    'Agent__Tools__0__Type=OpenApi'
    "Agent__Tools__0__Url=${shared_apim_address}/swagger/${TIMESHEET_SWAGGER_NAME:-timesheet}/swagger.json"
    "Agent__Tools__0__BaseAddress=${shared_apim_address}/${timesheet_api_path%/}/"
    "Agent__Tools__0__Audience=$TIMESHEET_API_AUDIENCE"
    "Agent__Tools__0__SpecificationSubscriptionKey=$TIMESHEET_SWAGGER_SUBSCRIPTION_KEY"
  )

  # Only read operations are available to the model until confirmation of write operations is implemented.
  read -r -a allowed_operations <<< "${TIMESHEET_ALLOWED_OPERATIONS:-get-periods get-tags get-timesheets get-last-projects search-projects}"
  for index in "${!allowed_operations[@]}"; do
    agent_settings+=("Agent__Tools__0__AllowedOperations__${index}=${allowed_operations[$index]}")
  done
fi

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
    "${agent_settings[@]}" \
  --output none \
  --only-show-errors

echo "Function App settings configured for $FUNCTION_APP_NAME."
