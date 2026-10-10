#!/bin/sh
# Reuse existing signing assets; never create/revoke certificates or profiles.
set -eu
ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
# Machine-specific settings stay local; environment variables can override them.
if [ -f "$ROOT/.local/iphone-signing.env" ]; then . "$ROOT/.local/iphone-signing.env"; fi
DEVICE="${ENDLESS_IPHONE_UDID:?Set ENDLESS_IPHONE_UDID in .local/iphone-signing.env}"
IDENTITY="${ENDLESS_SIGNING_IDENTITY:?Set ENDLESS_SIGNING_IDENTITY in .local/iphone-signing.env}"
PROFILE="${ENDLESS_PROVISIONING_PROFILE:?Set ENDLESS_PROVISIONING_PROFILE in .local/iphone-signing.env}"
printf '%s\n' 'Keep your iPhone connected and unlocked. Building Endless…'
"$ROOT/scripts/dotnet-apple.sh" build Endless.iOS.csproj -r ios-arm64 "-p:CodesignKey=$IDENTITY" "-p:CodesignProvision=$PROFILE"
APP="$ROOT/Apple/Endless.iOS/bin/Debug/net10.0-ios/ios-arm64/Endless.iOS.app"
xcrun devicectl device install app --device "$DEVICE" "$APP" --timeout 120
xcrun devicectl device process launch --device "$DEVICE" --terminate-existing dev.endlessapple.client --timeout 30
printf '%s\n' 'Endless has been updated and opened on your iPhone.'
