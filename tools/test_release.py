"""Regression tests of release tooling only; synthetic ZIP fixtures are NOT built .NET packages."""
from __future__ import annotations
import os
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch
import xml.etree.ElementTree as ET
import zipfile
import release

VERSION = '2.0.0-preview.4'
COMMIT = 'a' * 40


class ReleaseTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.directory = Path(self.temp.name)
        self.make_packages()

    def tearDown(self):
        self.temp.cleanup()

    def make_packages(self):
        # Only exercise the ZIP/metadata verifier; no fixture is copied into deliverable artifacts.
        for p in release.catalog():
            package = ET.Element('package')
            m = ET.SubElement(package, 'metadata')
            for key, val in {'id': p['id'], 'version': VERSION, 'authors': 'MiKiNuo',
                             'description': 'Synthetic verifier fixture, not a real assembly',
                             'readme': 'README.md'}.items():
                ET.SubElement(m, key).text = val
            ET.SubElement(m, 'license', type='expression').text = 'MIT'
            ET.SubElement(m, 'repository', type='git', url=release.REPOSITORY, commit=COMMIT)
            deps = ET.SubElement(ET.SubElement(m, 'dependencies'), 'group', targetFramework=p['tfm'])
            for dep in p['dependencies']:
                ET.SubElement(deps, 'dependency', id=dep, version=VERSION)
            entries = {p['id'] + '.nuspec': ET.tostring(package), 'README.md': b'fixture', 'LICENSE': b'MIT'}
            if p['kind'] == 'analyzer':
                entries.update({f'analyzers/dotnet/cs/{p["id"]}.dll': b'MZ-fixture-not-an-assembly',
                                f'buildTransitive/{p["id"]}.targets': b'<Project />',
                                f'config/{p["id"]}.globalconfig': b'is_global = true'})
            else:
                entries.update({f'lib/{p["tfm"]}/{p["id"]}.dll': b'MZ-fixture-not-an-assembly',
                                f'lib/{p["tfm"]}/{p["id"]}.xml': b'<doc />'})
                with zipfile.ZipFile(self.directory / f'{p["id"]}.{VERSION}.snupkg', 'w') as z:
                    z.writestr(f'lib/{p["tfm"]}/{p["id"]}.pdb', 'synthetic symbol fixture')
            with zipfile.ZipFile(self.directory / f'{p["id"]}.{VERSION}.nupkg', 'w') as z:
                for name, value in entries.items():
                    z.writestr(name, value)

    def modify(self, id, change):
        path = self.directory / f'{id}.{VERSION}.nupkg'
        with zipfile.ZipFile(path) as z:
            entries = {n: z.read(n) for n in z.namelist()}
        change(entries)
        with zipfile.ZipFile(path, 'w') as z:
            for name, value in entries.items(): z.writestr(name, value)

    def metadata(self, id, change):
        def update(entries):
            name = id + '.nuspec'
            tree = ET.fromstring(entries[name])
            change(tree.find('metadata'))
            entries[name] = ET.tostring(tree)
        self.modify(id, update)

    def verify(self):
        return release.verify_packages(self.directory, VERSION, True, COMMIT)

    def manifest(self):
        cases = [{'case': name, 'compiled': True, 'managed_probe_executed': run, 'passed': True}
                 for name, run in [('runtime', True), ('binding', True), ('avalonia', True),
                                   ('godot', False), ('both-platforms', False), ('transitive', True)]]
        cases.append({'case': 'invalid-binding', 'expected_diagnostic': 'MVI0200', 'passed': True})
        data = {'version': VERSION, 'build_and_tests_passed': True, 'git': {'commit': COMMIT, 'clean': True},
                'packages': self.verify(), 'consumers': cases,
                'files': {p.name: release.digest(p) for p in self.directory.iterdir()
                          if p.suffix in ('.nupkg', '.snupkg')}}
        release.save_json(self.directory / 'release-manifest.json', data)
        return data

    def test_versions_are_canonical_and_safe(self):
        for version in ('2.0.0', VERSION, '2.1.0-rc.2', '0.0.0-ci.12.1'):
            self.assertEqual(release.version_value(version), version)
        for version in ('../../x', 'v2.0.0', '2.0', '2.0.0+sha', '2.0.0;echo bad', '02.0.0',
                        '2.0.0-rc.02', '2.0.0-PREVIEW.4', ''):
            with self.subTest(version=version), self.assertRaises(ValueError):
                release.version_value(version)

    def test_six_packages_and_five_symbols_are_required(self):
        self.assertEqual(len(self.verify()), 6)
        next(self.directory.glob('*Godot*.nupkg')).unlink()
        with self.assertRaisesRegex(ValueError, 'Package set'): self.verify()

    def test_real_nuspec_namespace_is_supported(self):
        for item in release.catalog():
            def add_namespace(entries):
                name = item['id'] + '.nuspec'
                xml = ET.fromstring(entries[name])
                xml.set('xmlns', 'http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd')
                entries[name] = ET.tostring(xml)
            self.modify(item['id'], add_namespace)
        self.assertEqual(len(self.verify()), 6)

    def test_missing_symbol_is_rejected(self):
        next(self.directory.glob('*.snupkg')).unlink()
        with self.assertRaisesRegex(ValueError, 'symbol packages'): self.verify()

    def test_duplicate_generator_in_platform_is_rejected(self):
        self.modify('MiKiNuo.Mvi.Platforms.Avalonia', lambda e: e.update({
            f'analyzers/dotnet/cs/{release.GENERATOR}.dll': b'MZfixture'}))
        with self.assertRaises(ValueError): self.verify()

    def test_analyzer_with_runtime_lib_is_rejected(self):
        self.modify(release.GENERATOR, lambda e: e.update({'lib/netstandard2.0/wrong.dll': b'MZfixture'}))
        with self.assertRaisesRegex(ValueError, 'analyzer DLL'): self.verify()

    def test_missing_transitive_loader_is_rejected(self):
        self.modify(release.GENERATOR, lambda e: e.pop(f'buildTransitive/{release.GENERATOR}.targets'))
        with self.assertRaisesRegex(ValueError, 'targets/config'): self.verify()

    def test_compiler_dependencies_cannot_leak(self):
        self.metadata(release.GENERATOR, lambda m: ET.SubElement(m.find('dependencies/group'),
            'dependency', id='Microsoft.CodeAnalysis.CSharp', version='5.0.0'))
        with self.assertRaisesRegex(ValueError, 'Development dependencies'): self.verify()

    def test_private_generator_dependency_is_rejected(self):
        def exclude(m):
            for dep in m.iter('dependency'):
                if dep.get('id') == release.GENERATOR: dep.set('exclude', 'BuildTransitive')
        self.metadata('MiKiNuo.Mvi.Runtime', exclude)
        with self.assertRaisesRegex(ValueError, 'excluded'): self.verify()

    def test_mixed_own_dependency_versions_are_rejected(self):
        self.metadata('MiKiNuo.Mvi.Binding', lambda m: m.find('dependencies/group/dependency').set('version', '1.0.0'))
        with self.assertRaisesRegex(ValueError, 'Mixed release versions'): self.verify()

    def test_wrong_commit_is_rejected(self):
        self.metadata('MiKiNuo.Mvi.Runtime', lambda m: m.find('repository').set('commit', 'b'*40))
        with self.assertRaisesRegex(ValueError, 'commit differs'): self.verify()

    def test_missing_readme_is_rejected(self):
        self.modify('MiKiNuo.Mvi.Binding', lambda e: e.pop('README.md'))
        with self.assertRaisesRegex(ValueError, 'documentation'): self.verify()

    def test_path_traversal_is_rejected(self):
        self.modify('MiKiNuo.Mvi.Binding', lambda e: e.update({'../evil': b'x'}))
        with self.assertRaisesRegex(ValueError, 'Unsafe ZIP'): self.verify()

    def test_package_bytes_cannot_change_after_validation(self):
        self.manifest()
        self.modify('MiKiNuo.Mvi.Binding', lambda e: e.update({'README.md': b'changed'}))
        with self.assertRaisesRegex(ValueError, 'hashes'):
            release.verify_manifest(self.directory, VERSION)

    def test_dirty_or_unverified_release_cannot_publish(self):
        data = self.manifest()
        data['git']['clean'] = False
        release.save_json(self.directory / 'release-manifest.json', data)
        with self.assertRaisesRegex(ValueError, 'clean-commit'): release.verify_manifest(self.directory, VERSION)
        data['git']['clean'] = True
        data['build_and_tests_passed'] = False
        release.save_json(self.directory / 'release-manifest.json', data)
        with self.assertRaisesRegex(ValueError, 'clean-commit'): release.verify_manifest(self.directory, VERSION)

    def test_provenance_must_match_github_commit(self):
        self.manifest()
        with patch.dict(os.environ, {'GITHUB_SHA': 'b'*40}):
            with self.assertRaisesRegex(ValueError, 'provenance'): release.verify_manifest(self.directory, VERSION)
        with patch.dict(os.environ, {'GITHUB_SHA': COMMIT}):
            self.assertEqual(release.verify_manifest(self.directory, VERSION)['version'], VERSION)

    def test_native_failure_stops_the_pipeline(self):
        with self.assertRaisesRegex(RuntimeError, 'Exit 9'):
            release.execute([sys.executable, '-c', 'raise SystemExit(9)'], self.directory/'failure.log')

    def test_expected_error_must_really_fail(self):
        with self.assertRaisesRegex(RuntimeError, 'Expected failing'):
            release.execute([sys.executable, '-c', 'print("MVI0200")'], self.directory/'negative.log',
                            expected_error='MVI0200')

    def test_credential_arguments_not_written_to_logs(self):
        log = self.directory / 'private.log'
        release.execute([sys.executable, '-c', 'pass', 'synthetic-key'], log, private=True)
        self.assertNotIn('synthetic-key', log.read_text())

    def test_external_consumer_has_no_source_references(self):
        xml = ET.fromstring(release.consumer_project(['MiKiNuo.Mvi.Platforms.Avalonia'], VERSION, 'HAS_BINDING'))
        self.assertEqual(len(list(xml.iter('ProjectReference'))), 0)
        self.assertEqual(xml.find('ItemGroup/PackageReference').get('Version'), f'[{VERSION}]')
        self.assertEqual(xml.find('PropertyGroup/ManagePackageVersionsCentrally').text, 'false')
        self.assertIsNotNone(xml.find("Target[@Name='AssertPackagedGenerator']"))

    def test_external_policy_does_not_disable_mvi_contracts(self):
        config = (release.ROOT/'src/MiKiNuo.Mvi.Generators/config/MiKiNuo.Mvi.Generators.globalconfig').read_text()
        rules = [line for line in config.splitlines() if line.startswith('dotnet_diagnostic.')]
        self.assertGreater(len(rules), 0)
        self.assertTrue(all(line.split('.')[1].startswith(('DOC', 'CODE', 'ARCH')) for line in rules))
        self.assertNotIn('dotnet_diagnostic.MVI', config)

    def test_catalog_matches_the_six_project_ids(self):
        for entry in release.catalog():
            xml = ET.parse(release.ROOT/entry['project'])
            self.assertEqual(xml.find('.//PackageId').text, entry['id'])
        self.assertEqual(len(release.catalog()), 6)

    def test_public_targets_do_not_mutate_consumer_framework(self):
        for path in (release.ROOT/'src').glob('*/buildTransitive/*.targets'):
            xml = ET.parse(path)
            for forbidden in ('PackageReference', 'TargetFramework', 'TargetFrameworks', 'LangVersion', 'TreatWarningsAsErrors'):
                self.assertEqual(list(xml.iter(forbidden)), [], (path, forbidden))


if __name__ == '__main__':
    unittest.main()
