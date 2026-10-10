#!/bin/sh
set -eu
ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
cd "$ROOT"
mkdir -p .local
curl --fail --location --proto '=https' --tlsv1.2 https://dot.net/v1/dotnet-install.sh -o .local/dotnet-install.sh
bash .local/dotnet-install.sh --version 10.0.401 --architecture arm64 --install-dir .local/dotnet-apple --no-path
# Microsoft release dotnet-10.0.1xx-xcode27.0-10722 pins this workload set to Xcode 27.
scripts/dotnet-apple.sh workload install ios --version 10.0.401.1
