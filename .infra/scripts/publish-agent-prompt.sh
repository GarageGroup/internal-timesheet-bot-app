#!/usr/bin/env bash
set -euo pipefail

# Publishes the agent system prompt of a release package to Blob Storage. The bot reads the prompt from the blob,
# so it can be edited there without a deployment. Every deployment replaces the blob with the prompt from the code.

required=(
  AZURE_RESOURCE_GROUP_NAME
  STORAGE_ACCOUNT_NAME
  PACKAGE_PATH
)

for variable in "${required[@]}"; do
  [[ -n "${!variable:-}" ]] || { echo "Missing $variable" >&2; exit 1; }
done

container_name="${AGENT_PROMPT_CONTAINER_NAME:-agent}"
blob_name="${AGENT_PROMPT_BLOB_NAME:-AgentSystem.md}"

[[ -f "$PACKAGE_PATH" ]] || { echo "Function package was not found: $PACKAGE_PATH" >&2; exit 1; }

prompt_file="$(mktemp)"
trap 'rm -f "$prompt_file"' EXIT

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

if not content.strip():
    sys.exit('The agent prompt in the package is empty')

with open(prompt_path, 'wb') as prompt:
    prompt.write(content)
PYTHON

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
