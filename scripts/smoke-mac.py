#!/usr/bin/env python3
"""Bounded local-only startup check. This does not verify rendering or official-server play."""
import os
from pathlib import Path
import signal
import subprocess
import sys

root = Path(__file__).resolve().parents[1]
launcher = root / 'scripts/run-mac.sh'
log = root / 'artifacts/mac-startup.log'
log.parent.mkdir(exist_ok=True)
with log.open('w') as output:
    process = subprocess.Popen([str(launcher)], cwd=root, stdout=output,
                               stderr=subprocess.STDOUT, start_new_session=True)
    survived = False
    try:
        process.wait(timeout=8)
    except subprocess.TimeoutExpired:
        survived = True
    finally:
        if process.poll() is None:
            os.killpg(process.pid, signal.SIGTERM)
            try:
                process.wait(timeout=3)
            except subprocess.TimeoutExpired:
                os.killpg(process.pid, signal.SIGKILL)
                process.wait(timeout=3)
output = log.read_text()
print(output[-3500:])
if not survived or 'Exception thrown' in output:
    print('FAIL: client did not pass the bounded startup check.', file=sys.stderr)
    sys.exit(1)
print('PASS: client stayed running for 8 seconds; test process stopped. Rendering and gameplay remain unverified.')
