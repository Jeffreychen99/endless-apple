#!/bin/sh
set -eu
ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
export DOTNET_CLI_HOME="$ROOT/.local/dotnet-home"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
cd "$ROOT"
SDK_DIR="$ROOT/.local/dotnet"
if [ "${ENDLESS_DOTNET_ARCH:-arm64}" = "x64" ]; then SDK_DIR="$ROOT/.local/dotnet-x64"; fi
if [ -x "$SDK_DIR/dotnet" ]; then
  export DOTNET_ROOT="$SDK_DIR"
  export PATH="$DOTNET_ROOT:$PATH"
  exec "$DOTNET_ROOT/dotnet" "$@"
fi
exec dotnet "$@"
