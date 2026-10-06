"""Regression tests for the structural checker, not tests of the C# framework."""
from pathlib import Path
import shutil
import tempfile
import unittest
from verify_source import verify
ROOT=Path(__file__).resolve().parents[1]
class SourceChecks(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory()
        self.root=Path(self.temp.name)/'MVI'
        shutil.copytree(ROOT,self.root,ignore=shutil.ignore_patterns('artifacts','evidence','.git','bin','obj','__pycache__'))
    def tearDown(self): self.temp.cleanup()
    def test_valid_tree(self): self.assertTrue(verify(self.root)['passed'])
    def test_missing_project(self):
        (self.root/'src/MiKiNuo.Mvi.Runtime/MiKiNuo.Mvi.Runtime.csproj').unlink()
        self.assertFalse(verify(self.root)['passed'])
    def test_sidecar_version(self):
        (self.root/'src/MiKiNuo.Mvi.Runtime/V2').mkdir()
        self.assertFalse(verify(self.root)['passed'])
    def test_duplicate_store(self):
        (self.root/'src/MiKiNuo.Mvi.Runtime/Extra.cs').write_text('public sealed class MviStore<S,I> {}')
        self.assertFalse(verify(self.root)['passed'])
    def test_sensitive_state(self):
        p=self.root/'sample/MiKiNuo.Mvi.Samples.Avalonia/Features/Login/LoginState.cs'
        p.write_text(p.read_text()+'\npublic record Extra(string Password);\n')
        self.assertFalse(verify(self.root)['passed'])
    def test_local_server_reintroduced(self):
        (self.root/'sample/MiKiNuo.Mvi.Samples.Server').mkdir()
        self.assertFalse(verify(self.root)['passed'])
    def test_loopback_endpoint_reintroduced(self):
        p=self.root/'sample/MiKiNuo.Mvi.Samples.Avalonia/Features/Auth/HttpAuthService.cs'
        p.write_text(p.read_text().replace('https://dummyjson.com/', 'http://127.0.0.1:5188/'))
        self.assertFalse(verify(self.root)['passed'])
    def test_retired_recovery_protocol(self):
        p=self.root/'sample/MiKiNuo.Mvi.Samples.Avalonia/Features/Auth/Extra.cs'
        p.write_text('internal record RecoveryRequest(string Email);')
        self.assertFalse(verify(self.root)['passed'])
    def test_new_forwarding_method(self):
        p=self.root/'sample/MiKiNuo.Mvi.Samples.Avalonia/Features/Login/LoginViewModel.cs'
        p.write_text(p.read_text()+'\nprivate void SubmitAsync() {}\n')
        self.assertFalse(verify(self.root)['passed'])
    def test_unknown_diagnostic_constant(self):
        (self.root/'src/MiKiNuo.Mvi.Generators/Extra.cs').write_text('var id = DiagnosticIdCatalog.UnknownRule;')
        self.assertFalse(verify(self.root)['passed'])
if __name__=='__main__': unittest.main()
