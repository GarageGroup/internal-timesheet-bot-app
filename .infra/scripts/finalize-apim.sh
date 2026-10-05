#!/usr/bin/env bash
set -euo pipefail

required=(AZURE_SUBSCRIPTION_ID AZURE_RESOURCE_GROUP_NAME FUNCTION_APP_NAME BOT_APIM_SERVICE_NAME)
for variable in "${required[@]}"; do
  [[ -n "${!variable:-}" ]] || { echo "Missing $variable" >&2; exit 1; }
done

az account set --subscription "$AZURE_SUBSCRIPTION_ID"
function_master_key=''
for _ in {1..12}; do
  function_master_key="$(az functionapp keys list \
    --resource-group "$AZURE_RESOURCE_GROUP_NAME" \
    --name "$FUNCTION_APP_NAME" \
    --query masterKey \
    --output tsv \
    --only-show-errors 2>/dev/null || true)"
  [[ -n "$function_master_key" ]] && break
  sleep 10
done
[[ -n "$function_master_key" ]] || { echo 'Function App master key was not found after deployment' >&2; exit 1; }

named_value_url="https://management.azure.com/subscriptions/$AZURE_SUBSCRIPTION_ID/resourceGroups/$AZURE_RESOURCE_GROUP_NAME/providers/Microsoft.ApiManagement/service/$BOT_APIM_SERVICE_NAME/namedValues/BotFunctionCode"
current="$(az rest --method POST --url "$named_value_url/listValue?api-version=2024-05-01" --query value --output tsv --only-show-errors)"
if [[ "$current" != "$function_master_key" ]]; then
  body="$(jq -n --arg value "$function_master_key" '{properties: {displayName: "BotFunctionCode", secret: true, value: $value}}')"
  az rest --method PUT --url "$named_value_url?api-version=2024-05-01" --headers 'Content-Type=application/json' 'If-Match=*' --body "$body" --output none --only-show-errors
fi

echo "Bot APIM Function key configured for $FUNCTION_APP_NAME."
