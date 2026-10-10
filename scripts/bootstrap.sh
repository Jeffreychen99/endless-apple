#!/bin/sh
# Project-local .NET only; does not alter system SDKs or install Xcode.
set -eu
ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
cd "$ROOT"
mkdir -p .local
curl --fail --location --proto '=https' --tlsv1.2 https://dot.net/v1/dotnet-install.sh -o .local/dotnet-install.sh
bash .local/dotnet-install.sh --version 8.0.204 --architecture arm64 --install-dir .local/dotnet --no-path
# MonoGame 3.8.2 bundles an Intel-only FreeImage content compiler dependency.
bash .local/dotnet-install.sh --version 8.0.204 --architecture x64 --install-dir .local/dotnet-x64 --no-path
