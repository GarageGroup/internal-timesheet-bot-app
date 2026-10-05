#!/usr/bin/env bash
set -euo pipefail

required=(AZURE_SUBSCRIPTION_ID AZURE_RESOURCE_GROUP_NAME AZURE_LOCATION FUNCTION_APP_NAME API_WEB_APP_NAME BOT_APIM_SERVICE_NAME BOT_APIM_PUBLISHER_EMAIL BOT_APIM_PUBLISHER_NAME)
for variable in "${required[@]}"; do
  [[ -n "${!variable:-}" ]] || { echo "Missing $variable" >&2; exit 1; }
done

az account set --subscription "$AZURE_SUBSCRIPTION_ID"

telegram_bot_token="$(az webapp config appsettings list \
  --resource-group "$AZURE_RESOURCE_GROUP_NAME" \
  --name "$API_WEB_APP_NAME" \
  --query "[?name=='TelegramBot__Token'].value | [0]" \
  --output tsv \
  --only-show-errors)"
[[ -n "$telegram_bot_token" ]] || { echo 'Telegram bot token was not found in API Web App settings' >&2; exit 1; }

if ! az apim show --resource-group "$AZURE_RESOURCE_GROUP_NAME" --name "$BOT_APIM_SERVICE_NAME" --output none --only-show-errors 2>/dev/null; then
  az apim create \
    --resource-group "$AZURE_RESOURCE_GROUP_NAME" \
    --name "$BOT_APIM_SERVICE_NAME" \
    --location "${BOT_APIM_LOCATION:-$AZURE_LOCATION}" \
    --sku-name Consumption \
    --publisher-email "$BOT_APIM_PUBLISHER_EMAIL" \
    --publisher-name "$BOT_APIM_PUBLISHER_NAME" \
    --output none \
    --only-show-errors
fi

function_master_key="$(az functionapp keys list \
  --resource-group "$AZURE_RESOURCE_GROUP_NAME" \
  --name "$FUNCTION_APP_NAME" \
  --query masterKey \
  --output tsv \
  --only-show-errors 2>/dev/null || true)"
if [[ -z "$function_master_key" ]]; then
  existing_url="https://management.azure.com/subscriptions/$AZURE_SUBSCRIPTION_ID/resourceGroups/$AZURE_RESOURCE_GROUP_NAME/providers/Microsoft.ApiManagement/service/$BOT_APIM_SERVICE_NAME/namedValues/BotFunctionCode/listValue?api-version=2024-05-01"
  function_master_key="$(az rest --method POST --url "$existing_url" --query value --output tsv --only-show-errors 2>/dev/null || true)"
fi
function_master_key="${function_master_key:-pending-bootstrap-$(cat /proc/sys/kernel/random/uuid)}"

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
az deployment group create \
  --resource-group "$AZURE_RESOURCE_GROUP_NAME" \
  --name "timesheet-bot-apim-${ENVIRONMENT_NAME:-environment}" \
  --mode Incremental \
  --template-file "$script_dir/../apim/main.bicep" \
  --parameters \
    "apimServiceName=$BOT_APIM_SERVICE_NAME" \
    "functionAppName=$FUNCTION_APP_NAME" \
    "telegramBotToken=$telegram_bot_token" \
    "functionMasterKey=$function_master_key" \
  --output none \
  --only-show-errors

apim_scope="/subscriptions/$AZURE_SUBSCRIPTION_ID/resourceGroups/$AZURE_RESOURCE_GROUP_NAME/providers/Microsoft.ApiManagement/service/$BOT_APIM_SERVICE_NAME"
for subscription in 'TelegramApiSubscription:gtimesheet-telegram-api' 'TelegramBotSubscription:gtimesheet-bot-api'; do
  subscription_name="${subscription%%:*}"
  api_name="${subscription#*:}"
  url="https://management.azure.com${apim_scope}/subscriptions/${subscription_name}?api-version=2024-05-01"
  existing="$(az rest --method GET --url "$url" --query name --output tsv --only-show-errors 2>/dev/null || true)"
  if [[ -n "$existing" ]]; then
    continue
  fi
  body="$(jq -n --arg name "$subscription_name" --arg scope "$apim_scope/apis/$api_name" \
    '{properties: {displayName: $name, scope: $scope, state: "active"}}')"
  az rest --method PUT --url "$url" --headers 'Content-Type=application/json' --body "$body" --output none --only-show-errors
done

echo "Timesheet Bot APIM '$BOT_APIM_SERVICE_NAME' configured."
