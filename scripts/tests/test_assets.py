import importlib.util
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('assets', Path(__file__).parents[1] / 'audit-assets.py')
assets = importlib.util.module_from_spec(spec)
spec.loader.exec_module(assets)

class AuditTests(unittest.TestCase):
    def test_inventory_excludes_secrets_and_links(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / 'data').mkdir()
            (root / 'data' / 'map.emf').write_bytes(b'EMF\x01\x02')
            (root / 'data' / 'table.epf').write_bytes(b'EPF\x01')
            (root / 'data' / 'account.ini').write_text('password=not-for-report')
            (root / 'outside').mkdir()
            (root / 'outside' / 'secret.epf').write_text('never index symlink')
            (root / 'data' / 'link.epf').symlink_to(root / 'outside' / 'secret.epf')
            report = assets.audit(root)
            self.assertEqual(report['counts'], {'.emf': 1, '.epf': 1})
            self.assertEqual(len(report['files']), 2)
            self.assertTrue(report['findings'])
            self.assertNotIn('not-for-report', str(report))
            self.assertNotIn('secret.epf', str(report))
            self.assertTrue(all(len(f['sha256']) == 64 for f in report['files']))

    def test_absent_assets_are_not_claimed_compatible(self):
        with tempfile.TemporaryDirectory() as directory:
            report = assets.audit(Path(directory))
            self.assertEqual(report['counts'], {})
            self.assertIn('do not prove', report['compatibility'])

if __name__ == '__main__':
    unittest.main()
