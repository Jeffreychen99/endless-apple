#!/bin/sh
# A local developer bundle; depends on this checkout's private .NET runtime.
set -eu
ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
CLIENT="$ROOT/bin/Release/client/net8.0"
APP="$ROOT/artifacts/Endless Apple.app"
if [ ! -f "$CLIENT/EndlessClient.dll" ]; then
  echo 'Run scripts/build-mac.sh first.' >&2
  exit 1
fi
mkdir -p "$APP/Contents/MacOS"
ditto "$CLIENT" "$APP/Contents/MacOS"
cat > "$APP/Contents/Info.plist" <<'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
<key>CFBundleExecutable</key><string>launch</string>
<key>CFBundleIdentifier</key><string>dev.endlessapple.preview</string>
<key>CFBundleName</key><string>Endless Apple</string>
<key>CFBundleDisplayName</key><string>Endless Apple — Classic Preview</string>
<key>CFBundlePackageType</key><string>APPL</string>
<key>CFBundleShortVersionString</key><string>0.1.0</string>
<key>CFBundleVersion</key><string>1</string>
<key>NSHighResolutionCapable</key><true/>
</dict></plist>
PLIST
cat > "$APP/Contents/MacOS/launch" <<'LAUNCH'
#!/bin/sh
set -eu
HERE=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
ROOT=$(CDPATH= cd -- "$HERE/../../../.." && pwd)
export DOTNET_ROOT="$ROOT/.local/dotnet-x64"
export ENDLESS_RESOURCE_ROOT="$HERE"
export ENDLESS_USER_ROOT="$ROOT/.local/.endlessclient"
mkdir -p "$ENDLESS_USER_ROOT" "$HERE/Contents/Resources"
if [ ! -e "$HERE/Contents/Resources/ContentPipeline" ]; then
  ln -s ../../ContentPipeline "$HERE/Contents/Resources/ContentPipeline"
fi
cd "$HERE"
exec ./EndlessClient --host 127.0.0.1 "$@"
LAUNCH
chmod +x "$APP/Contents/MacOS/launch"
plutil -lint "$APP/Contents/Info.plist"
printf 'Local developer bundle: %s\n' "$APP"
