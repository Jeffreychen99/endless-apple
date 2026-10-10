#!/bin/sh
set -eu
ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
cd "$ROOT"
# Upstream MonoGame's content compiler depends on Intel FreeImage.
export ENDLESS_DOTNET_ARCH=x64
exec scripts/dotnet-local.sh build reference/monogame/EndlessClient/EndlessClient.csproj -p:PortableDesktop=true -p:UseSharedCompilation=false -c Release "$@"
