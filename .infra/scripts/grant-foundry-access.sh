#!/usr/bin/env bash
set -euo pipefail

# Lets the agent Managed Identity of the bot call the chat and speech-to-text models of the Azure AI Foundry account.

required=(
  FOUNDRY_ACCOUNT_ID
  AGENT_MANAGED_IDENTITY_PRINCIPAL_ID
)

for variable in "${required[@]}"; do
  [[ -n "${!variable:-}" ]] || { echo "Missing $variable" >&2; exit 1; }
done

role='Cognitive Services User'

existing="$(az role assignment list \
  --assignee-object-id "$AGENT_MANAGED_IDENTITY_PRINCIPAL_ID" \
  --scope "$FOUNDRY_ACCOUNT_ID" \
  --role "$role" \
  --query '[0].id' \
  --output tsv \
  --only-show-errors)"

if [[ -n "$existing" ]]; then
  echo "Role '$role' is already assigned."
  exit 0
fi

# Creating a role assignment requires Microsoft.Authorization/roleAssignments/write on the Foundry account.
az role assignment create \
  --assignee-object-id "$AGENT_MANAGED_IDENTITY_PRINCIPAL_ID" \
  --assignee-principal-type ServicePrincipal \
  --role "$role" \
  --scope "$FOUNDRY_ACCOUNT_ID" \
  --output none \
  --only-show-errors
echo "Role '$role' has been assigned."
