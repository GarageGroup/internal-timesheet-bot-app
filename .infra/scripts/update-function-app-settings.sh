#!/usr/bin/env bash
set -euo pipefail

# Updates the settings of the existing Function App during a release deployment, so a new setting of a release
# does not need a separate run of the installation. The resources are not changed: their values are only read.
#
# VARS_JSON is the 'vars' context of GitHub Actions as JSON. Every variable becomes an environment variable
# unless the step has already set it, so the list of variables is kept in one place: in the scripts.

[[ -n "${VARS_JSON:-}" ]] || { echo 'Missing VARS_JSON' >&2; exit 1; }

while IFS= read -r -d '' entry; do
  name="${entry%%=*}"
  [[ -n "${!name:-}" ]] || export "$entry"
done < <(jq -j 'to_entries[] | select(.key | test("^[A-Z][A-Z0-9_]*$")) | "\(.key)=\(.value)\u0000"' <<< "$VARS_JSON")

# The scripts name these two values differently from the GitHub variables.
export AZURE_SUBSCRIPTION_ID="${AZURE_SUBSCRIPTION_ID:-${DEPLOY_SUBSCRIPTION_ID:-}}"
export ENVIRONMENT_NAME="${ENVIRONMENT_NAME:-${AZURE_NAME_POSTFIX:-}}"

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
resources_file="$(mktemp)"
trap 'rm -f "$resources_file"' EXIT

DEPLOY_INFRASTRUCTURE=false GITHUB_OUTPUT="$resources_file" bash "$script_dir/install-azure-resources.sh"

resource_value() {
  sed -n "s/^$1=//p" "$resources_file" | tail -n 1
}

FUNCTION_APP_NAME="$(resource_value function_app_name)"
DATAVERSE_MANAGED_IDENTITY_CLIENT_ID="$(resource_value dataverse_identity_client_id)"
AGENT_MANAGED_IDENTITY_CLIENT_ID="$(resource_value agent_identity_client_id)"
FOUNDRY_PROJECT_ENDPOINT="$(resource_value foundry_project_endpoint)"
FOUNDRY_OPENAI_ENDPOINT="$(resource_value foundry_openai_endpoint)"
FOUNDRY_CHAT_DEPLOYMENT_NAME="$(resource_value chat_deployment_name)"
FOUNDRY_VOICE_DEPLOYMENT_NAME="$(resource_value voice_deployment_name)"
FOUNDRY_VOICE_MODEL_NAME="$(resource_value voice_model_name)"

export FUNCTION_APP_NAME DATAVERSE_MANAGED_IDENTITY_CLIENT_ID AGENT_MANAGED_IDENTITY_CLIENT_ID
export FOUNDRY_PROJECT_ENDPOINT FOUNDRY_OPENAI_ENDPOINT
export FOUNDRY_CHAT_DEPLOYMENT_NAME FOUNDRY_VOICE_DEPLOYMENT_NAME FOUNDRY_VOICE_MODEL_NAME

bash "$script_dir/set-function-app-settings.sh"
