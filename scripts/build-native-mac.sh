#!/bin/sh
set -eu
ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
exec "$ROOT/scripts/dotnet-apple.sh" build ../Endless.Mac/Endless.Mac.csproj -p:RuntimeIdentifier=maccatalyst-arm64 "$@"
