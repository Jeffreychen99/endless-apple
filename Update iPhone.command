#!/bin/sh
ROOT=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
"$ROOT/scripts/update-iphone.sh"
RESULT=$?
if [ "$RESULT" -ne 0 ]; then
  printf '\n%s\n' 'Update did not complete. Keep this window open to inspect the error. Existing signing assets were reused; no certificates were created or revoked.'
fi
printf '\n%s' 'Press Return to close… '
read -r ANSWER
exit "$RESULT"
