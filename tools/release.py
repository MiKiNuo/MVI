#!/usr/bin/env python3
"""Build, validate and publish one coherent release. Requires Python 3.10+ and .NET 10 SDK."""
from __future__ import annotations
import argparse
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import shutil
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
import zipfile
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[1]
REPOSITORY = "https://github.com/MiKiNuo/MVI"
GENERATOR = "MiKiNuo.Mvi.Generators"


def version_value(value: str) -> str:
    """Use canonical SemVer: no path characters, leading zeroes or build metadata."""
    number = r"(?:0|[1-9][0-9]*)"
    if not re.fullmatch(rf"{number}\.{number}\.{number}(?:-[0-9a-z-]+(?:\.[0-9a-z-]+)*)?", value):
        raise ValueError("Version must be canonical SemVer, e.g. 2.0.0 or 2.0.0-preview.4.")
    suffix = value.partition("-")[2]
    if any(part.isdigit() and len(part) > 1 and part[0] == "0" for part in suffix.split(".")):
        raise ValueError("Numeric prerelease identifiers cannot have leading zeroes.")
    return value


def catalog() -> list[dict]:
    return json.loads((ROOT / "eng/packages.json").read_text(encoding="utf-8"))


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def save_json(path: Path, value: object) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def execute(args: list[str], log: Path, cwd: Path = ROOT, env: dict | None = None,
            expected_error: str | None = None, private: bool = False) -> None:
    """A native nonzero exit must not be mistaken for a successful PowerShell pipeline."""
    log.parent.mkdir(parents=True, exist_ok=True)
    print("=== " + log.stem + " ===", flush=True)
    with log.open("w", encoding="utf-8") as output:
        output.write("command: " + ("[credential-bearing command omitted]" if private else repr(args)) + "\n")
        output.flush()
        result = subprocess.run(args, cwd=cwd, env=env, stdout=output, stderr=subprocess.STDOUT, check=False)
    text = log.read_text(encoding="utf-8", errors="replace")
    print(text[-5000:], flush=True)
    if expected_error is not None:
        if result.returncode == 0 or expected_error not in text:
            raise RuntimeError(f"Expected failing diagnostic {expected_error}; see {log}.")
    elif result.returncode != 0:
        raise RuntimeError(f"Exit {result.returncode}: see {log}.")


def check_dependencies(dependencies: list[ET.Element], expected: list[str], version: str) -> None:
    own = {e.attrib["id"]: e for e in dependencies if e.attrib["id"].startswith("MiKiNuo.Mvi.")}
    if set(own) != set(expected):
        raise ValueError(f"Unexpected internal dependency graph: {set(own)}; expected {set(expected)}")
    for name, item in own.items():
        # SDK ProjectReferences normally produce a minimum version; validate its exact lower bound.
        minimum = item.attrib.get("version", "").strip("[]() ").split(",")[0].strip()
        if minimum != version:
            raise ValueError(f"Mixed release versions: {name} -> {item.attrib.get('version')}, expected {version}")
        if name == GENERATOR:
            excluded = {p.strip().lower() for p in item.attrib.get("exclude", "").split(",")}
            included = {p.strip().lower() for p in item.attrib.get("include", "all").split(",")}
            if "all" in excluded or "buildtransitive" in excluded:
                raise ValueError("Generator buildTransitive assets were excluded.")
            if "all" not in included and "buildtransitive" not in included and "build" not in included:
                raise ValueError("Generator dependency does not include build assets.")


def verify_packages(directory: Path, version: str, require_commit: bool = False,
                    expected_commit: str = "") -> list[dict]:
    """Inspect real nupkg contents; this never claims to replace a consumer build."""
    expected = catalog()
    package_names = {p["id"] + "." + version + ".nupkg" for p in expected}
    actual = {p.name for p in directory.glob("*.nupkg")}
    if actual != package_names:
        raise ValueError(f"Package set mismatch. Missing={package_names-actual}; extra={actual-package_names}")
    symbols = {p["id"] + "." + version + ".snupkg" for p in expected if p["kind"] == "library"}
    if {p.name for p in directory.glob('*.snupkg')} != symbols:
        raise ValueError("Expected symbol packages for the five runtime libraries only.")
    inspected = []
    for package in expected:
        path = directory / (package["id"] + "." + version + ".nupkg")
        with zipfile.ZipFile(path) as archive:
            names = archive.namelist()
            if len(names) != len(set(names)) or archive.testzip() is not None:
                raise ValueError(f"Duplicate entry or bad ZIP CRC in {path.name}")
            if any(PurePosixPath(n).is_absolute() or ".." in PurePosixPath(n).parts or "\\" in n for n in names):
                raise ValueError(f"Unsafe ZIP entry in {path.name}")
            nuspecs = [n for n in names if n.endswith(".nuspec")]
            if len(nuspecs) != 1:
                raise ValueError("Exactly one nuspec is required.")
            metadata = ET.fromstring(archive.read(nuspecs[0])).find("{*}metadata")
            if metadata is None:
                raise ValueError("Missing nuspec metadata.")
            def text(name: str) -> str:
                node = metadata.find("{*}" + name)
                return "" if node is None else (node.text or "").strip()
            if text("id") != package["id"] or text("version") != version:
                raise ValueError(f"Wrong id/version inside {path.name}")
            license_node = metadata.find("{*}license")
            repository_node = metadata.find("{*}repository")
            if (not text("authors") or text("license") != "MIT" or license_node is None
                    or license_node.attrib.get("type") != "expression" or not text("description")
                    or text("readme") != "README.md" or "README.md" not in names or "LICENSE" not in names):
                raise ValueError(f"Missing package documentation/license metadata in {path.name}")
            if repository_node is None or repository_node.attrib.get("url", "").removesuffix(".git") != REPOSITORY:
                raise ValueError(f"Repository metadata missing from {path.name}")
            if require_commit and not re.fullmatch(r"[0-9a-f]{40}", repository_node.attrib.get("commit", "")):
                raise ValueError(f"Repository commit missing from release package {path.name}")
            if expected_commit and repository_node.attrib.get("commit") != expected_commit:
                raise ValueError(f"Package commit differs from the tested source: {path.name}")
            deps = metadata.findall(".//{*}dependency")
            check_dependencies(deps, package["dependencies"], version)
            if any(e.attrib["id"].startswith(("Microsoft.CodeAnalysis", "TUnit", "BenchmarkDotNet")) for e in deps):
                raise ValueError(f"Development dependencies leaked from {path.name}")
            if package["kind"] == "analyzer" and deps:
                raise ValueError("Generator compiler dependencies must not be NuGet runtime dependencies.")
            dlls = [n for n in names if n.endswith(".dll")]
            if package["kind"] == "analyzer":
                required = {f"analyzers/dotnet/cs/{GENERATOR}.dll", f"buildTransitive/{GENERATOR}.targets",
                            f"config/{GENERATOR}.globalconfig"}
                if not required.issubset(names) or len(dlls) != 1 or any(n.startswith(("lib/", "ref/")) for n in names):
                    raise ValueError("Generator must have one analyzer DLL, targets/config, and no lib/ref assets.")
            else:
                required_dll = f"lib/{package['tfm']}/{package['id']}.dll"
                if dlls != [required_dll] or f"lib/{package['tfm']}/{package['id']}.xml" not in names:
                    raise ValueError(f"Wrong or missing runtime/XML assets in {path.name}")
                if any(n.startswith("analyzers/") for n in names):
                    raise ValueError("A runtime/platform package contains a duplicate generator.")
                with zipfile.ZipFile(directory / (package["id"] + "." + version + ".snupkg")) as symbol_zip:
                    if symbol_zip.testzip() is not None or not any(n.endswith(".pdb") for n in symbol_zip.namelist()):
                        raise ValueError(f"Invalid symbol package for {package['id']}")
            if any(not archive.read(n).startswith(b"MZ") for n in dlls):
                raise ValueError("An assembly entry is not a PE file.")
        inspected.append({"id": package["id"], "version": version, "file": path.name, "sha256": digest(path)})
    return inspected


def consumer_project(references: list[str], version: str, flags: str, generator_required: bool = True,
                     output: str = "Exe", package_id: str = "") -> str:
    refs = "\n".join(f'    <PackageReference Include="{name}" Version="[{version}]" />' for name in references)
    check = """
  <Target Name="AssertPackagedGenerator" BeforeTargets="CoreCompile" DependsOnTargets="MiKiNuoMviLoadPackagedGenerators">
    <ItemGroup>
      <_ProbeGenerators Include="@(Analyzer)" Condition="'%(Analyzer.Filename)' == 'MiKiNuo.Mvi.Generators'" />
    </ItemGroup>
    <Error Condition="'@(_ProbeGenerators->Count())' != '1'" Text="The generator must load exactly once from NuGet." />
  </Target>""" if generator_required else ""
    return f"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework><LangVersion>14.0</LangVersion>
    <Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings>
    <OutputType>{output}</OutputType><AssemblyName>PackageProbe</AssemblyName>
    <RootNamespace>PackageProbe</RootNamespace><TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
    <DefineConstants>$(DefineConstants);{flags}</DefineConstants>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)generated</CompilerGeneratedFilesOutputPath>
    <IsPackable>{str(bool(package_id)).lower()}</IsPackable>
    <PackageId>{package_id or 'PackageProbe'}</PackageId><Version>{version}</Version>
    <Description>Isolated package consumption fixture, never published.</Description>
    <Authors>MiKiNuo</Authors><PackageLicenseExpression>MIT</PackageLicenseExpression>
    <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
  </PropertyGroup>
  <ItemGroup>
{refs}
  </ItemGroup>{check}
</Project>
"""


def consumer_tests(directory: Path, version: str, logs: Path) -> list[dict]:
    template = ROOT / "test/PackageConsumer"
    results = []
    with tempfile.TemporaryDirectory(prefix="mvi-nuget-") as temp:
        isolated = Path(temp)
        # These barriers prevent importing any machine/parent/repository policy accidentally.
        for name in ("Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props"):
            (isolated / name).write_text("<Project />\n", encoding="utf-8")
        (isolated / ".editorconfig").write_text("root = true\n", encoding="utf-8")
        shutil.copy2(ROOT / "global.json", isolated / "global.json")
        feed = isolated / "feed"
        feed.mkdir()
        for package in directory.glob("*.nupkg"):
            shutil.copy2(package, feed / package.name)
        config = isolated / "NuGet.config"
        config.write_text(f"""<configuration>
  <fallbackPackageFolders><clear /></fallbackPackageFolders>
  <packageSources><clear /><add key="local" value="{escape(str(feed), {chr(34): '&quot;'})}" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources>
  <packageSourceMapping><clear />
    <packageSource key="local"><package pattern="MiKiNuo.Mvi.*" /></packageSource>
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
  </packageSourceMapping>
</configuration>""", encoding="utf-8")
        env = dict(os.environ, NUGET_PACKAGES=str(isolated / "cache"))
        common = ["--configfile", str(config), "--no-http-cache"]
        # The facade is another nupkg, not a ProjectReference into this repository.
        facade = isolated / "facade"
        facade.mkdir()
        facade_id = "MiKiNuo.Mvi.PackageProbeFacade"
        (facade / "Facade.csproj").write_text(consumer_project(["MiKiNuo.Mvi.Binding"], version, "",
                    False, "Library", facade_id), encoding="utf-8")
        (facade / "Facade.cs").write_text("public sealed class FacadeMarker { }\n", encoding="utf-8")
        execute(["dotnet", "restore", "Facade.csproj", *common], logs / "facade-restore.log", facade, env)
        execute(["dotnet", "pack", "Facade.csproj", "-c", "Release", "--no-restore", "-o", str(feed)],
                logs / "facade-pack.log", facade, env)
        cases = [
            ("runtime", ["MiKiNuo.Mvi.Runtime"], "", True),
            ("binding", ["MiKiNuo.Mvi.Binding"], "HAS_BINDING", True),
            ("avalonia", ["MiKiNuo.Mvi.Platforms.Avalonia"], "HAS_BINDING;HAS_AVALONIA", True),
            ("godot", ["MiKiNuo.Mvi.Platforms.Godot"], "HAS_BINDING;HAS_GODOT", False),
            ("both-platforms", ["MiKiNuo.Mvi.Platforms.Avalonia", "MiKiNuo.Mvi.Platforms.Godot", GENERATOR],
                "HAS_BINDING;HAS_AVALONIA;HAS_GODOT", False),
            ("transitive", [facade_id], "HAS_BINDING", True),
        ]
        for name, refs, flags, run in cases:
            case = isolated / name
            case.mkdir()
            (case / "Consumer.csproj").write_text(consumer_project(refs, version, flags), encoding="utf-8")
            shutil.copy2(template / "Counter.cs.template", case / "Counter.cs")
            if "HAS_AVALONIA" in flags:
                for item in ("ProbeView.axaml", "ProbeView.axaml.cs"):
                    shutil.copy2(template / (item + ".template"), case / item)
            if "HAS_GODOT" in flags:
                shutil.copy2(template / "ProbeGodotView.cs.template", case / "ProbeGodotView.cs")
            execute(["dotnet", "restore", "Consumer.csproj", *common], logs / f"{name}-restore.log", case, env)
            execute(["dotnet", "build", "Consumer.csproj", "-c", "Release", "--no-restore"],
                    logs / f"{name}-build.log", case, env)
            output = case / "bin/Release/net10.0"
            if list(output.glob("MiKiNuo.Mvi.Generators.dll")) or list(output.glob("Microsoft.CodeAnalysis*.dll")):
                raise ValueError("Build-time tool leaked into the consumer runtime output.")
            assets = json.loads((case / "obj/project.assets.json").read_text(encoding="utf-8"))
            if any(value.get("type") == "project" for value in assets["libraries"].values()):
                raise ValueError("A consumer restored source projects instead of packages.")
            if run:
                execute(["dotnet", str(output / "PackageProbe.dll")], logs / f"{name}-run.log", case, env)
                if "PACKAGE_CONSUMER_OK" not in (logs / f"{name}-run.log").read_text(encoding="utf-8"):
                    raise ValueError("Consumer probe did not reach its final assertion.")
            results.append({"case": name, "compiled": True, "managed_probe_executed": run, "passed": True})
        # Public policy defaults must not disable the framework's actual contract diagnostics.
        negative = isolated / "binding"
        source = negative / "Counter.cs"
        source.write_text(source.read_text(encoding="utf-8").replace("[MviBind]\n", '[MviBind("NoSuchStateMember")]\n', 1), encoding="utf-8")
        execute(["dotnet", "build", "Consumer.csproj", "-c", "Release", "--no-restore"],
                logs / "invalid-binding.log", negative, env, expected_error="MVI0200")
        results.append({"case": "invalid-binding", "expected_diagnostic": "MVI0200", "passed": True})
    return results


def git_info() -> dict:
    def git(*args: str) -> str:
        result = subprocess.run(["git", *args], cwd=ROOT, capture_output=True, text=True, check=False)
        return result.stdout.strip() if result.returncode == 0 else ""
    if not shutil.which("git"):
        return {"commit": "", "clean": False}
    commit = git("rev-parse", "HEAD")
    status = subprocess.run(["git", "status", "--porcelain"], cwd=ROOT, capture_output=True, text=True, check=False)
    return {"commit": commit, "clean": bool(commit) and status.returncode == 0 and not status.stdout.strip()}


def pack(version: str) -> None:
    if not shutil.which("dotnet"):
        raise FileNotFoundError(".NET SDK not found. Install a .NET 10 SDK; no build or package was produced.")
    directory = ROOT / "artifacts/packages" / version
    logs = ROOT / "artifacts/release" / version
    if directory.exists():
        shutil.rmtree(directory)
    directory.mkdir(parents=True)
    git = git_info()
    version_args = [f"-p:Version={version}", f"-p:PackageVersion={version}"]
    execute(["dotnet", "--info"], logs / "environment.log")
    execute(["dotnet", "restore", "MiKiNuo.Mvi.slnx", *version_args], logs / "restore.log")
    execute(["dotnet", "build", "MiKiNuo.Mvi.slnx", "-c", "Release", "--no-restore", *version_args], logs / "build.log")
    execute(["dotnet", "run", "--project", "test/MiKiNuo.Mvi.Tests", "-c", "Release", "--no-build", "--no-restore",
             "--", "--maximum-parallel-tests", "1", "--results-directory", str(logs / "test-results")], logs / "tests.log")
    execute(["dotnet", "run", "--project", "test/MiKiNuo.Mvi.Benchmarks", "-c", "Release", "--no-build", "--no-restore",
             "--", "--list", "flat"], logs / "benchmark-discovery.log")
    for item in catalog():
        execute(["dotnet", "pack", item["project"], "-c", "Release", "--no-build", "--no-restore", *version_args,
                 "-o", str(directory)], logs / f"pack-{item['id']}.log")
    inspected = verify_packages(directory, version, require_commit=os.environ.get("GITHUB_ACTIONS") == "true",
                                expected_commit=git["commit"])
    consumers = consumer_tests(directory, version, logs / "consumers")
    if git_info() != git:
        raise ValueError("Tracked source or Git commit changed while building the release.")
    hashes = {path.name: digest(path) for path in sorted(directory.iterdir()) if path.suffix in (".nupkg", ".snupkg")}
    save_json(directory / "release-manifest.json", {"version": version, "git": git,
              "build_and_tests_passed": True, "packages": inspected, "consumers": consumers, "files": hashes})
    (directory / "SHA256SUMS").write_text("".join(f"{sha}  {name}\n" for name, sha in hashes.items()), encoding="utf-8")
    print(f"All release gates passed. Packages: {directory}")


def verify_manifest(directory: Path, version: str) -> dict:
    manifest = json.loads((directory / "release-manifest.json").read_text(encoding="utf-8"))
    if (manifest.get("version") != version or manifest.get("build_and_tests_passed") is not True
            or manifest.get("git", {}).get("clean") is not True or len(manifest.get("consumers", [])) != 7
            or not all(case.get("passed") is True for case in manifest["consumers"])):
        raise ValueError("Only a clean-commit release with all build/consumer gates passed can be published.")
    consumers = {case.get("case"): case for case in manifest["consumers"]}
    positives = {"runtime", "binding", "avalonia", "godot", "both-platforms", "transitive"}
    if (set(consumers) != positives | {"invalid-binding"}
            or any(consumers[name].get("compiled") is not True for name in positives)
            or any(consumers[name].get("managed_probe_executed") is not True
                   for name in ("runtime", "binding", "avalonia", "transitive"))
            or consumers["invalid-binding"].get("expected_diagnostic") != "MVI0200"):
        raise ValueError("Incomplete or incorrectly recorded consumer gates.")
    expected_hashes = manifest["files"]
    actual_files = {p.name: digest(p) for p in directory.iterdir() if p.suffix in (".nupkg", ".snupkg")}
    if actual_files != expected_hashes:
        raise ValueError("Package hashes no longer match the tested artifacts.")
    commit = manifest["git"].get("commit", "")
    if not re.fullmatch(r"[0-9a-f]{40}", commit):
        raise ValueError("Release manifest has no valid Git commit.")
    verify_packages(directory, version, require_commit=True, expected_commit=commit)
    if os.environ.get("GITHUB_SHA") and commit != os.environ["GITHUB_SHA"]:
        raise ValueError("Package provenance does not match the current GitHub commit.")
    return manifest


def publish(directory: Path, version: str) -> None:
    verify_manifest(directory, version)
    key = os.environ.get("NUGET_API_KEY", "")
    if not key:
        raise ValueError("Missing NUGET_API_KEY; use NuGet/login OIDC or a GitHub secret.")
    # Only the tested artifacts are pushed. dotnet also pushes each matching .snupkg.
    for item in catalog():
        execute(["dotnet", "nuget", "push", str(directory / f"{item['id']}.{version}.nupkg"),
                 "--source", "https://api.nuget.org/v3/index.json", "--api-key", key, "--skip-duplicate"],
                ROOT / "artifacts/publish" / version / f"{item['id']}.log", private=True)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=("pack", "verify", "verify-release", "publish", "version"))
    parser.add_argument("--version", required=True, type=version_value)
    parser.add_argument("--directory", type=Path)
    args = parser.parse_args()
    try:
        if args.command == "pack":
            pack(args.version)
        elif args.command == "publish":
            publish((args.directory or ROOT / "artifacts/packages" / args.version).resolve(), args.version)
        elif args.command == "verify-release":
            verify_manifest((args.directory or ROOT / "artifacts/packages" / args.version).resolve(), args.version)
            print("Tested artifact hashes and provenance are valid.")
        elif args.command == "verify":
            print(json.dumps(verify_packages((args.directory or ROOT / "artifacts/packages" / args.version).resolve(), args.version), indent=2))
        else:
            print(args.version)
        return 0
    except (OSError, RuntimeError, ValueError, KeyError, ET.ParseError, zipfile.BadZipFile) as error:
        print(f"RELEASE FAILED: {error}", file=sys.stderr)
        return 127 if isinstance(error, FileNotFoundError) else 1


if __name__ == "__main__":
    raise SystemExit(main())
