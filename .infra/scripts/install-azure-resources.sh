#!/usr/bin/env bash
set -euo pipefail

required=(
  AZURE_SUBSCRIPTION_ID
  AZURE_RESOURCE_GROUP_NAME
  AZURE_LOCATION
  ENVIRONMENT_NAME
  FUNCTION_APP_NAME
  APP_SERVICE_PLAN_NAME
  STORAGE_ACCOUNT_NAME
  APPLICATION_INSIGHTS_NAME
  DATAVERSE_MANAGED_IDENTITY_NAME
)

for variable in "${required[@]}"; do
  [[ -n "${!variable:-}" ]] || { echo "Missing $variable" >&2; exit 1; }
done

az account set --subscription "$AZURE_SUBSCRIPTION_ID"

for provider in Microsoft.Web Microsoft.Storage Microsoft.Insights Microsoft.ManagedIdentity Microsoft.ApiManagement; do
  state="$(az provider show --namespace "$provider" --query registrationState --output tsv --only-show-errors)"
  [[ "$state" == 'Registered' ]] || { echo "Azure resource provider is not registered: $provider" >&2; exit 1; }
done

if [[ "$(az group exists --name "$AZURE_RESOURCE_GROUP_NAME" --output tsv --only-show-errors)" == false ]]; then
  az group create \
    --name "$AZURE_RESOURCE_GROUP_NAME" \
    --location "$AZURE_LOCATION" \
    --tags 'application=garage-timesheet-bot' "environment=$ENVIRONMENT_NAME" 'managedBy=github-actions' \
    --output none \
    --only-show-errors
fi

existing_function="$(az functionapp show \
  --resource-group "$AZURE_RESOURCE_GROUP_NAME" \
  --name "$FUNCTION_APP_NAME" \
  --output json \
  --only-show-errors 2>/dev/null || true)"
function_location="$(jq -r '.location // empty' <<< "${existing_function:-null}")"
function_location="${function_location:-$AZURE_LOCATION}"
function_tags="$(jq -c \
  --arg environment "$ENVIRONMENT_NAME" \
  'if type == "object" then (.tags // {}) else {
    application: "garage-timesheet-bot",
    environment: $environment,
    managedBy: "github-actions"
  } end' <<< "${existing_function:-null}")"
function_tags_base64="$(printf '%s' "$function_tags" | base64 --wrap=0)"

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
outputs="$(az deployment group create \
  --resource-group "$AZURE_RESOURCE_GROUP_NAME" \
  --name "timesheet-bot-${ENVIRONMENT_NAME}" \
  --mode Incremental \
  --template-file "$script_dir/../main.bicep" \
  --parameters \
    "location=$AZURE_LOCATION" \
    "functionLocation=$function_location" \
    "functionAppName=$FUNCTION_APP_NAME" \
    "appServicePlanName=$APP_SERVICE_PLAN_NAME" \
    "storageAccountName=$STORAGE_ACCOUNT_NAME" \
    "applicationInsightsName=$APPLICATION_INSIGHTS_NAME" \
    "dataverseManagedIdentityName=$DATAVERSE_MANAGED_IDENTITY_NAME" \
    "functionTagsBase64=$function_tags_base64" \
  --query properties.outputs \
  --output json \
  --only-show-errors)"

if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
  jq -r '
    "function_app_name=" + .functionAppName.value,
    "function_app_host_name=" + .functionAppHostName.value,
    "function_principal_id=" + .functionPrincipalId.value,
    "dataverse_identity_client_id=" + .dataverseIdentityClientId.value,
    "agent_identity_client_id=" + .agentIdentityClientId.value,
    "application_insights_connection_string=" + .applicationInsightsConnectionString.value
  ' <<< "$outputs" >> "$GITHUB_OUTPUT"
fi

echo "Infrastructure deployment timesheet-bot-${ENVIRONMENT_NAME} completed."
