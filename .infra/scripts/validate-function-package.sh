#!/usr/bin/env bash
set -euo pipefail

package="${1:?Usage: validate-function-package.sh <publish-directory-or-zip>}"

if [[ -d "$package" ]]; then
  read_file() { cat "$package/$1"; }
  [[ -f "$package/host.json" && -f "$package/worker.config.json" && -f "$package/functions.metadata" ]] || {
    echo 'Function publish output is missing host.json, worker.config.json or functions.metadata' >&2
    exit 1
  }
elif [[ -f "$package" && "$package" == *.zip ]]; then
  entries="$(unzip -Z -1 "$package")"
  zip_root=''
  if grep -Fxq './host.json' <<< "$entries"; then
    zip_root='./'
  fi
  read_file() { unzip -p "$package" "${zip_root}$1"; }
  for entry in host.json worker.config.json functions.metadata; do
    grep -Fxq "${zip_root}$entry" <<< "$entries" || { echo "Function ZIP is missing root entry: $entry" >&2; exit 1; }
  done
  if grep -Eq '\\|(^|/)local\.settings\.json$' <<< "$entries"; then
    echo 'Function ZIP contains Windows paths or local.settings.json' >&2
    exit 1
  fi
else
  echo "Function package was not found: $package" >&2
  exit 1
fi

read_file worker.config.json | jq -e '.description.language == "dotnet-isolated" and .description.workerIndexing == "false"' > /dev/null || {
  echo 'Function worker indexing must be disabled for this .NET 10 application' >&2
  exit 1
}

for name in HealthCheck HandleBotHttp HandleBotEntity; do
  read_file functions.metadata | jq -e --arg name "$name" 'any(.[]; .name == $name)' > /dev/null || {
    echo "Function metadata is missing $name" >&2
    exit 1
  }
done

echo 'Function package structure and metadata are valid.'
