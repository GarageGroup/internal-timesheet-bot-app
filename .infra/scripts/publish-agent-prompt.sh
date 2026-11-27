#!/usr/bin/env bash
set -euo pipefail

# Publishes the agent system prompt of a release package to Blob Storage. The bot reads the prompt from the blob,
# so it can be edited there without a deployment. Every deployment replaces the blob with the prompt from the code.

required=(
  AZURE_RESOURCE_GROUP_NAME
  STORAGE_ACCOUNT_NAME
)

for variable in "${required[@]}"; do
  [[ -n "${!variable:-}" ]] || { echo "Missing $variable" >&2; exit 1; }
done

# The prompt comes either from a file of the repository (PROMPT_FILE) or from a release package (PACKAGE_PATH).
[[ -n "${PROMPT_FILE:-}" || -n "${PACKAGE_PATH:-}" ]] || { echo 'Missing PROMPT_FILE or PACKAGE_PATH' >&2; exit 1; }

container_name="${AGENT_PROMPT_CONTAINER_NAME:-agent}"
blob_name="${AGENT_PROMPT_BLOB_NAME:-AgentSystem.md}"

prompt_file="$(mktemp)"
trap 'rm -f "$prompt_file"' EXIT

if [[ -n "${PROMPT_FILE:-}" ]]; then
  [[ -f "$PROMPT_FILE" ]] || { echo "Prompt file was not found: $PROMPT_FILE" >&2; exit 1; }
  cp "$PROMPT_FILE" "$prompt_file"
else
  [[ -f "$PACKAGE_PATH" ]] || { echo "Function package was not found: $PACKAGE_PATH" >&2; exit 1; }

  python3 - "$PACKAGE_PATH" "$prompt_file" <<'PYTHON'
import sys
import zipfile

package_path, prompt_path = sys.argv[1], sys.argv[2]

with zipfile.ZipFile(package_path) as package:
    names = {name[2:] if name.startswith('./') else name: name for name in package.namelist()}
    entry = names.get('Prompts/AgentSystem.md')
    if entry is None:
        sys.exit('The package does not contain Prompts/AgentSystem.md')
    content = package.read(entry)

with open(prompt_path, 'wb') as prompt:
    prompt.write(content)
PYTHON
fi

[[ -n "$(tr -d '[:space:]' < "$prompt_file")" ]] || { echo 'The agent prompt is empty' >&2; exit 1; }

storage_key="$(az storage account keys list \
  --resource-group "$AZURE_RESOURCE_GROUP_NAME" \
  --account-name "$STORAGE_ACCOUNT_NAME" \
  --query '[0].value' \
  --output tsv \
  --only-show-errors)"
[[ -n "$storage_key" ]] || { echo 'Storage account key was not found' >&2; exit 1; }

az storage container create \
  --name "$container_name" \
  --account-name "$STORAGE_ACCOUNT_NAME" \
  --account-key "$storage_key" \
  --output none \
  --only-show-errors

az storage blob upload \
  --container-name "$container_name" \
  --name "$blob_name" \
  --file "$prompt_file" \
  --content-type 'text/markdown; charset=utf-8' \
  --overwrite true \
  --account-name "$STORAGE_ACCOUNT_NAME" \
  --account-key "$storage_key" \
  --output none \
  --only-show-errors

echo "Agent prompt published to $container_name/$blob_name."
