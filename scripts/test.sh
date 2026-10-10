#!/bin/sh
set -eu
ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
cd "$ROOT"
scripts/dotnet-local.sh run --project engine/apple/Endless.Protocol.Tests -p:UseSharedCompilation=false
python3 -m unittest discover -s scripts/tests -v
