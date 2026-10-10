#!/bin/sh
# Runs the classic desktop baseline. This is NOT current official-server compatibility.
set -eu
ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
CLIENT="$ROOT/bin/Release/client/net8.0"
if [ ! -f "$CLIENT/EndlessClient.dll" ]; then
  echo 'Run scripts/build-mac.sh first.' >&2
  exit 1
fi
export ENDLESS_RESOURCE_ROOT="$CLIENT"
export ENDLESS_USER_ROOT="$ROOT/.local/.endlessclient"
export DOTNET_ROOT="$ROOT/.local/dotnet-x64"
mkdir -p "$ENDLESS_USER_ROOT" "$CLIENT/Contents/Resources"
# XNAControls resolves fonts inside the conventional macOS bundle resource path.
if [ ! -e "$CLIENT/Contents/Resources/ContentPipeline" ]; then
  ln -s ../../ContentPipeline "$CLIENT/Contents/Resources/ContentPipeline"
fi
cd "$CLIENT"
# Explicit overrides may select a compatible development server. Default is local only.
exec "$DOTNET_ROOT/dotnet" EndlessClient.dll --host 127.0.0.1 "$@"
