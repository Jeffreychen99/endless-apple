#!/bin/sh
set -eu
ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
export DOTNET_ROOT="$ROOT/.local/dotnet-apple"
export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_CLI_HOME="$ROOT/.local/dotnet-apple-home"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
if [ ! -x "$DOTNET_ROOT/dotnet" ]; then
  echo 'Apple SDK missing. Run scripts/bootstrap-apple.sh.' >&2
  exit 1
fi
# SDK resolution must use the Apple target's pinned global.json, not upstream .NET 8.
cd "$ROOT/ios/Endless.iOS"
exec "$DOTNET_ROOT/dotnet" "$@"
