#!/usr/bin/env bash
set -euo pipefail

required=(AZURE_SUBSCRIPTION_ID AZURE_RESOURCE_GROUP_NAME API_WEB_APP_NAME BOT_APIM_SERVICE_NAME)
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

subscription_url="https://management.azure.com/subscriptions/$AZURE_SUBSCRIPTION_ID/resourceGroups/$AZURE_RESOURCE_GROUP_NAME/providers/Microsoft.ApiManagement/service/$BOT_APIM_SERVICE_NAME/subscriptions/TelegramBotSubscription/listSecrets?api-version=2024-05-01"
subscription_key="$(az rest --method POST --url "$subscription_url" --query primaryKey --output tsv --only-show-errors)"
[[ -n "$subscription_key" ]] || { echo 'Bot APIM subscription key was not found' >&2; exit 1; }

webhook_url="https://${BOT_APIM_SERVICE_NAME}.azure-api.net/bot/message?subscription-key=${subscription_key}"
telegram_api="https://api.telegram.org/bot${telegram_bot_token}"
current="$(curl --fail --silent --show-error "${telegram_api}/getWebhookInfo" | jq -er '.result.url')"

if [[ "$current" != "$webhook_url" ]]; then
  curl --fail --silent --show-error --request POST "${telegram_api}/setWebhook" \
    --data-urlencode "url=$webhook_url" | jq -e '.ok == true' >/dev/null
fi

verified="$(curl --fail --silent --show-error "${telegram_api}/getWebhookInfo" | jq -er '.result.url')"
[[ "$verified" == "$webhook_url" ]] || { echo 'Telegram webhook verification failed' >&2; exit 1; }

echo "Telegram webhook points to $BOT_APIM_SERVICE_NAME."
