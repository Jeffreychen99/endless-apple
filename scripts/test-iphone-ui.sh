#!/bin/sh
# Simulator-only UI verification. No real credentials, signing changes, or phone install.
set -eu
ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
cd "$ROOT"
if [ "$#" -ne 1 ]; then
  echo "Usage: scripts/test-iphone-ui.sh SIMULATOR-UDID" >&2
  echo "Find a booted iPhone simulator with: xcrun simctl list devices booted" >&2
  exit 2
fi
SIMULATOR_ID=$1
RESULT="$ROOT/artifacts/ui-tests-$(date +%Y%m%d-%H%M%S).xcresult"
scripts/dotnet-apple.sh build Endless.iOS.csproj -p:RuntimeIdentifier=iossimulator-arm64 --no-restore
xcrun simctl install "$SIMULATOR_ID" Apple/Endless.iOS/bin/Debug/net10.0-ios/iossimulator-arm64/Endless.iOS.app
xcodebuild test -project Apple/Endless.UITests/Endless.UITests.xcodeproj \
  -scheme EndlessUITests -destination "platform=iOS Simulator,id=$SIMULATOR_ID" \
  -derivedDataPath .local/ui-test-build -resultBundlePath "$RESULT" \
  -parallel-testing-enabled NO -collect-test-diagnostics never
