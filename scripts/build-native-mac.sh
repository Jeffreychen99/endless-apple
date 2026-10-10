#!/bin/sh
set -eu
ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
exec "$ROOT/scripts/dotnet-apple.sh" build ../../mac/Endless.Mac/Endless.Mac.csproj -p:RuntimeIdentifier=maccatalyst-arm64 "$@"
