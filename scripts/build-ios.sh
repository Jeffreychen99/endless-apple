#!/bin/sh
set -eu
ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
export DEVELOPER_DIR="${DEVELOPER_DIR:-/Applications/Xcode.app/Contents/Developer}"
if [ ! -d "$DEVELOPER_DIR" ]; then
  echo 'Full Xcode is missing. Complete the App Store download and Xcode first launch.' >&2
  exit 1
fi
# Never accept Apple's license automatically. xcodebuild reports any required user step.
xcodebuild -version
# Fail once with Apple's actionable message before a long native build.
xcrun --sdk iphonesimulator clang --version >/dev/null
exec "$ROOT/scripts/dotnet-apple.sh" build Endless.iOS.csproj -r iossimulator-arm64 "$@"
