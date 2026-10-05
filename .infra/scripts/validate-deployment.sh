#!/usr/bin/env bash
set -euo pipefail

required=(AZURE_RESOURCE_GROUP_NAME FUNCTION_APP_NAME)
for variable in "${required[@]}"; do
  [[ -n "${!variable:-}" ]] || { echo "Missing $variable" >&2; exit 1; }
done

state="$(az functionapp show \
  --resource-group "$AZURE_RESOURCE_GROUP_NAME" \
  --name "$FUNCTION_APP_NAME" \
  --query state \
  --output tsv \
  --only-show-errors)"
[[ "$state" == 'Running' ]] || { echo "Function App state is '$state', expected 'Running'" >&2; exit 1; }

principal_id="$(az functionapp identity show \
  --resource-group "$AZURE_RESOURCE_GROUP_NAME" \
  --name "$FUNCTION_APP_NAME" \
  --query principalId \
  --output tsv \
  --only-show-errors)"
[[ -n "$principal_id" ]] || { echo 'System Assigned Managed Identity was not found' >&2; exit 1; }

runtime="$(az functionapp config show \
  --resource-group "$AZURE_RESOURCE_GROUP_NAME" \
  --name "$FUNCTION_APP_NAME" \
  --query linuxFxVersion \
  --output tsv \
  --only-show-errors)"
[[ "$runtime" == 'DOTNET-ISOLATED|10.0' ]] || { echo "Unexpected runtime: $runtime" >&2; exit 1; }

echo "Function App $FUNCTION_APP_NAME infrastructure is ready."
