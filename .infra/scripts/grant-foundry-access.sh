#!/usr/bin/env bash
set -euo pipefail

# Lets the bot agent Managed Identity call the chat model of the Azure AI Foundry account used by the Timesheet API.

required=(
  AZURE_RESOURCE_GROUP_NAME
  API_WEB_APP_NAME
  AGENT_MANAGED_IDENTITY_PRINCIPAL_ID
)

for variable in "${required[@]}"; do
  [[ -n "${!variable:-}" ]] || { echo "Missing $variable" >&2; exit 1; }
done

role='Cognitive Services User'

foundry_endpoint="$(az webapp config appsettings list \
  --resource-group "$AZURE_RESOURCE_GROUP_NAME" \
  --name "$API_WEB_APP_NAME" \
  --query "[?name=='Agent__Foundry__ProjectEndpoint'].value | [0]" \
  --output tsv \
  --only-show-errors)"
[[ -n "$foundry_endpoint" ]] || { echo 'Foundry project endpoint was not found in API Web App settings' >&2; exit 1; }

# The endpoint has the form https://<account>.services.ai.azure.com/api/projects/<project>
foundry_host="${foundry_endpoint#https://}"
foundry_account_name="${foundry_host%%.*}"

foundry_id="$(az cognitiveservices account list \
  --query "[?name=='$foundry_account_name'].id | [0]" \
  --output tsv \
  --only-show-errors)"
[[ -n "$foundry_id" ]] || { echo "Foundry account '$foundry_account_name' was not found in the subscription" >&2; exit 1; }

existing="$(az role assignment list \
  --assignee-object-id "$AGENT_MANAGED_IDENTITY_PRINCIPAL_ID" \
  --scope "$foundry_id" \
  --role "$role" \
  --query '[0].id' \
  --output tsv \
  --only-show-errors)"

if [[ -z "$existing" ]]; then
  az role assignment create \
    --assignee-object-id "$AGENT_MANAGED_IDENTITY_PRINCIPAL_ID" \
    --assignee-principal-type ServicePrincipal \
    --role "$role" \
    --scope "$foundry_id" \
    --output none \
    --only-show-errors
  echo "Role '$role' has been assigned on $foundry_account_name."
else
  echo "Role '$role' is already assigned on $foundry_account_name."
fi
