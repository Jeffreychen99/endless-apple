#!/usr/bin/env python3
"""Install and open the built preview in an available iPhone simulator."""
import json
import os
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / 'Apple/Endless.iOS/bin/Debug/net10.0-ios/iossimulator-arm64/Endless.iOS.app'
ENV = dict(os.environ, DEVELOPER_DIR=os.environ.get('DEVELOPER_DIR', '/Applications/Xcode.app/Contents/Developer'))


def simctl(*args, capture=False, timeout=60):
    return subprocess.run(['xcrun', 'simctl', *args], env=ENV, check=True,
                          text=True, capture_output=capture, timeout=timeout)


def main():
    if not (APP / 'Endless.iOS').is_file():
        raise RuntimeError('Build the app first with scripts/build-ios.sh.')
    devices = json.loads(simctl('list', 'devices', 'available', '--json', capture=True).stdout)
    phones = [d for runtime, entries in devices['devices'].items() if '.iOS-' in runtime
              for d in entries if d['name'].startswith('iPhone')]
    requested = sys.argv[1] if len(sys.argv) > 1 else None
    if requested:
        phones = [d for d in phones if requested in (d['udid'], d['name'])]
    if not phones:
        raise RuntimeError('No matching iPhone simulator. Install an iOS runtime in Xcode.')
    device = next((d for d in phones if d['state'] == 'Booted'), phones[0])
    identifier = device['udid']
    print(f"Opening {device['name']} ({identifier})", flush=True)
    if device['state'] != 'Booted':
        simctl('boot', identifier)
    simctl('bootstatus', identifier, '-b', timeout=240)
    simctl('install', identifier, str(APP))
    developer = Path(ENV['DEVELOPER_DIR'])
    interfaces = [developer / 'Applications/Simulator.app',
                  developer.parent / 'Applications/Simulator.app',
                  developer.parent / 'Applications/DeviceHub.app']
    interface = next((path for path in interfaces if path.exists()), None)
    if interface:
        subprocess.run(['open', '-a', str(interface)], check=True, timeout=15)
    else:
        print('Simulator UI not found; launching on the booted virtual device.', flush=True)
    simctl('launch', '--terminate-running-process', identifier, 'dev.endlessapple.client')


if __name__ == '__main__':
    try:
        main()
    except (RuntimeError, subprocess.CalledProcessError, subprocess.TimeoutExpired) as error:
        print(f'Simulator launch failed: {error}', file=sys.stderr)
        sys.exit(1)
