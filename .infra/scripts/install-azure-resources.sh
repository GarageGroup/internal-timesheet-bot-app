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
  FOUNDRY_ACCOUNT_NAME
  FOUNDRY_PROJECT_NAME
)

for variable in "${required[@]}"; do
  [[ -n "${!variable:-}" ]] || { echo "Missing $variable" >&2; exit 1; }
done

az account set --subscription "$AZURE_SUBSCRIPTION_ID"

for provider in Microsoft.Web Microsoft.Storage Microsoft.Insights Microsoft.ManagedIdentity Microsoft.ApiManagement Microsoft.CognitiveServices; do
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

# Existing resources keep their regions and tags: a deployment cannot move a resource to another region.
agent_identity_location="$(az identity show \
  --resource-group "$AZURE_RESOURCE_GROUP_NAME" \
  --name "${FUNCTION_APP_NAME}-agent" \
  --query location \
  --output tsv \
  --only-show-errors 2>/dev/null || true)"
agent_identity_location="${agent_identity_location:-$AZURE_LOCATION}"

FOUNDRY_SUBSCRIPTION_ID="${FOUNDRY_SUBSCRIPTION_ID:-$AZURE_SUBSCRIPTION_ID}"
FOUNDRY_RESOURCE_GROUP="${FOUNDRY_RESOURCE_GROUP:-$AZURE_RESOURCE_GROUP_NAME}"
existing_foundry="$(az cognitiveservices account show \
  --resource-group "$FOUNDRY_RESOURCE_GROUP" \
  --name "$FOUNDRY_ACCOUNT_NAME" \
  --subscription "$FOUNDRY_SUBSCRIPTION_ID" \
  --output json \
  --only-show-errors 2>/dev/null || true)"
foundry_location="$(jq -r '.location // empty' <<< "${existing_foundry:-null}")"
foundry_location="${foundry_location:-${FOUNDRY_LOCATION:-$AZURE_LOCATION}}"
foundry_tags="$(jq -c \
  --arg environment "$ENVIRONMENT_NAME" \
  'if type == "object" then (.tags // {}) else {
    application: "garage-timesheet-bot",
    environment: $environment,
    managedBy: "github-actions"
  } end' <<< "${existing_foundry:-null}")"
foundry_tags_base64="$(printf '%s' "$foundry_tags" | base64 --wrap=0)"

if [[ "${DEPLOY_INFRASTRUCTURE:-true}" == 'true' ]]; then
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
      "agentIdentityLocation=$agent_identity_location" \
      "environmentName=$ENVIRONMENT_NAME" \
      "foundryAccountName=$FOUNDRY_ACCOUNT_NAME" \
      "foundryProjectName=$FOUNDRY_PROJECT_NAME" \
      "foundryLocation=$foundry_location" \
      "foundrySubscriptionId=$FOUNDRY_SUBSCRIPTION_ID" \
      "foundryResourceGroupName=$FOUNDRY_RESOURCE_GROUP" \
      "foundryTagsBase64=$foundry_tags_base64" \
      "chatDeploymentName=${FOUNDRY_CHAT_DEPLOYMENT_NAME:-gpt-5-mini}" \
      "chatModelName=${FOUNDRY_CHAT_MODEL_NAME:-gpt-5-mini}" \
      "chatModelVersion=${FOUNDRY_CHAT_MODEL_VERSION:-2025-08-07}" \
      "chatDeploymentSkuName=${FOUNDRY_CHAT_SKU_NAME:-GlobalStandard}" \
      "chatDeploymentCapacity=${FOUNDRY_CHAT_CAPACITY:-500}" \
      "voiceDeploymentName=${FOUNDRY_VOICE_DEPLOYMENT_NAME:-whisper}" \
      "voiceModelName=${FOUNDRY_VOICE_MODEL_NAME:-whisper}" \
      "voiceModelVersion=${FOUNDRY_VOICE_MODEL_VERSION:-001}" \
      "voiceDeploymentSkuName=${FOUNDRY_VOICE_SKU_NAME:-Standard}" \
      "voiceDeploymentCapacity=${FOUNDRY_VOICE_CAPACITY:-3}" \
    --query properties.outputs \
    --output json \
    --only-show-errors)"

else
  # The deployment is skipped: the values the next steps need are read from the existing resources.
  identity_value() {
    az identity show --resource-group "$AZURE_RESOURCE_GROUP_NAME" --name "$1" --query "$2" --output tsv --only-show-errors
  }

  [[ -n "${existing_function:-}" ]] || { echo "Function App '$FUNCTION_APP_NAME' does not exist: run the installation with infrastructure" >&2; exit 1; }
  [[ -n "${existing_foundry:-}" ]] || { echo "Foundry account '$FOUNDRY_ACCOUNT_NAME' does not exist: run the installation with infrastructure" >&2; exit 1; }

  outputs="$(jq -n \
    --arg functionAppName "$FUNCTION_APP_NAME" \
    --arg functionAppHostName "$(jq -r '.defaultHostName' <<< "$existing_function")" \
    --arg functionPrincipalId "$(jq -r '.identity.principalId // empty' <<< "$existing_function")" \
    --arg dataverseIdentityClientId "$(identity_value "$DATAVERSE_MANAGED_IDENTITY_NAME" clientId)" \
    --arg agentIdentityClientId "$(identity_value "${FUNCTION_APP_NAME}-agent" clientId)" \
    --arg agentIdentityPrincipalId "$(identity_value "${FUNCTION_APP_NAME}-agent" principalId)" \
    --arg foundryAccountId "$(jq -r '.id' <<< "$existing_foundry")" \
    --arg foundryProjectEndpoint "https://${FOUNDRY_ACCOUNT_NAME}.services.ai.azure.com/api/projects/${FOUNDRY_PROJECT_NAME}" \
    --arg foundryOpenAiEndpoint "https://${FOUNDRY_ACCOUNT_NAME}.openai.azure.com/" \
    --arg chatDeploymentName "${FOUNDRY_CHAT_DEPLOYMENT_NAME:-gpt-5-mini}" \
    --arg voiceDeploymentName "${FOUNDRY_VOICE_DEPLOYMENT_NAME:-whisper}" \
    --arg voiceModelName "${FOUNDRY_VOICE_MODEL_NAME:-whisper}" \
    '$ARGS.named | with_entries(.value = {value: .value}) + {applicationInsightsConnectionString: {value: ""}}')"
fi

if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
  jq -r '
    "function_app_name=" + .functionAppName.value,
    "function_app_host_name=" + .functionAppHostName.value,
    "function_principal_id=" + .functionPrincipalId.value,
    "dataverse_identity_client_id=" + .dataverseIdentityClientId.value,
    "agent_identity_client_id=" + .agentIdentityClientId.value,
    "agent_identity_principal_id=" + .agentIdentityPrincipalId.value,
    "foundry_account_id=" + .foundryAccountId.value,
    "foundry_project_endpoint=" + .foundryProjectEndpoint.value,
    "foundry_openai_endpoint=" + .foundryOpenAiEndpoint.value,
    "chat_deployment_name=" + .chatDeploymentName.value,
    "voice_deployment_name=" + .voiceDeploymentName.value,
    "voice_model_name=" + .voiceModelName.value,
    "application_insights_connection_string=" + .applicationInsightsConnectionString.value
  ' <<< "$outputs" >> "$GITHUB_OUTPUT"
fi

if [[ "${DEPLOY_INFRASTRUCTURE:-true}" == 'true' ]]; then
  echo "Infrastructure deployment timesheet-bot-${ENVIRONMENT_NAME} completed."
else
  echo 'Infrastructure deployment skipped: existing resources resolved.'
fi
