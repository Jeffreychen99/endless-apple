#!/bin/sh
# Explicit asset preparation; normal builds stay offline.
set -eu
ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
python3 -m venv "$ROOT/.local/font-tools"
"$ROOT/.local/font-tools/bin/pip" install fonttools==4.66.1 pillow==12.3.0
"$ROOT/.local/font-tools/bin/python" "$ROOT/scripts/build-ui-font.py"
curl -fL https://raw.githubusercontent.com/google/fonts/main/ofl/carterone/CarterOne.ttf -o "$ROOT/.local/ui-fonts/CarterOne.ttf"
curl -fL https://raw.githubusercontent.com/google/fonts/main/ofl/carterone/OFL.txt -o "$ROOT/.local/ui-fonts/CarterOne-OFL.txt"
