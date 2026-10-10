#!/bin/sh
set -eu
ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
APP="$ROOT/mac/Endless.Mac/bin/Debug/net10.0-maccatalyst/maccatalyst-arm64/Endless Apple.app"
if [ ! -d "$APP" ]; then "$ROOT/scripts/build-native-mac.sh"; fi
exec open "$APP" --args "$@"
