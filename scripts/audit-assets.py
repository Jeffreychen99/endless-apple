#!/usr/bin/env python3
"""Inventory a user-supplied client directory. Never copy assets or read account configuration."""
import argparse
import collections
import hashlib
import json
from pathlib import Path

ALLOWED = {'.emf', '.egf', '.epf', '.eif', '.enf', '.esf', '.ecf', '.edf', '.wav', '.mid', '.ess'}

def audit(root):
    root = root.resolve(strict=True)
    if not root.is_dir():
        raise ValueError('Expected a client directory')
    files, counts = [], collections.Counter()
    for folder in ('data', 'gfx', 'sfx', 'mfx'):
        for path in sorted((root / folder).rglob('*')):
            if path.is_symlink() or not path.is_file() or path.suffix.lower() not in ALLOWED:
                continue
            if not path.resolve().is_relative_to(root):
                continue
            digest = hashlib.sha256()
            signature = b''
            with path.open('rb') as stream:
                while block := stream.read(1024 * 1024):
                    if not signature:
                        signature = block[:8]
                    digest.update(block)
            counts[path.suffix.lower()] += 1
            files.append({'path': path.relative_to(root).as_posix(), 'bytes': path.stat().st_size,
                          'signature': signature.hex(), 'sha256': digest.hexdigest()})
    return {'counts': dict(sorted(counts.items())), 'files': files,
            'compatibility': 'Inventory only; signatures do not prove parser compatibility.',
            'findings': ['EPF data tables need a current-format parser. Classic EIF/ENF readers are insufficient.'] if counts['.epf'] else []}

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('client_directory', type=Path)
    args = parser.parse_args()
    print(json.dumps(audit(args.client_directory), indent=2))
