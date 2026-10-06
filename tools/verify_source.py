#!/usr/bin/env python3
"""Check project layout/references and migration invariants, NOT C# compilation."""
from __future__ import annotations
import argparse
import hashlib
import json
import re
from pathlib import Path
import xml.etree.ElementTree as ET

FRAMEWORK = {'MiKiNuo.Mvi.Abstractions','MiKiNuo.Mvi.Runtime','MiKiNuo.Mvi.Binding','MiKiNuo.Mvi.Generators','MiKiNuo.Mvi.Platforms.Avalonia','MiKiNuo.Mvi.Platforms.Godot'}

def verify(root: Path) -> dict:
    errors: list[str] = []
    projects = {str(p.relative_to(root)).replace('\\', '/'): p for p in root.rglob('*.csproj') if not {'bin','obj'}.intersection(p.parts)}
    solution = ET.parse(root/'MiKiNuo.Mvi.slnx')
    declared = {e.attrib['Path'].replace('\\','/') for e in solution.iter('Project')}
    if declared != set(projects): errors.append('Solution/project mismatch: '+repr(declared.symmetric_difference(projects)))
    if {p.parent.name for p in projects.values() if p.parent.parent.name == 'src'} != FRAMEWORK: errors.append('Framework project layout is not the upgraded six-layer layout.')
    sample_projects = {name for name in projects if name.startswith('sample/')}
    if sample_projects != {'sample/MiKiNuo.Mvi.Samples.Avalonia/MiKiNuo.Mvi.Samples.Avalonia.csproj'}:
        errors.append('The example must contain only the desktop project, with no local server.')
    if {p.name for p in (root/'sample').iterdir() if p.is_dir()} != {'MiKiNuo.Mvi.Samples.Avalonia'}:
        errors.append('Unexpected sample directory; no sidecar server or shared server protocol is needed.')
    graph: dict[str,list[str]] = {}
    for name, path in projects.items():
        tree = ET.parse(path); graph[name] = []
        for ref in tree.iter('ProjectReference'):
            target = (path.parent/ref.attrib['Include'].replace('\\','/')).resolve()
            if not target.is_file(): errors.append(f'Missing project reference: {name}: {target}')
            else: graph[name].append(str(target.relative_to(root.resolve())).replace('\\','/'))
        if path.parent.name == 'MiKiNuo.Mvi.Runtime' and any('Binding' in v or 'Platforms' in v for v in graph[name]): errors.append('Runtime depends on the UI binding/platform layer.')
        if path.parent.name == 'MiKiNuo.Mvi.Binding' and any('Platforms' in v for v in graph[name]): errors.append('Binding depends on a concrete platform.')
        for compile_item in tree.iter('Compile'):
            include = compile_item.attrib.get('Include','')
            if include and '$' not in include and '*' not in include and not (path.parent/include.replace('\\','/')).is_file(): errors.append(f'Missing linked source: {name}: {include}')
    visiting, complete = set(),set()
    def visit(name: str) -> None:
        if name in visiting: errors.append('Project reference cycle: '+name); return
        if name in complete: return
        visiting.add(name)
        for dependency in graph.get(name,[]): visit(dependency)
        visiting.remove(name); complete.add(name)
    for name in graph: visit(name)
    sources = [p for p in root.rglob('*.cs') if not {'bin','obj'}.intersection(p.parts)]
    prod = [p for p in sources if p.relative_to(root).parts[0] in ('src','sample')]
    for path in root.rglob('*'):
        if path.is_dir() and path.name.lower() in ('v2','authenticationv2'): errors.append('Parallel implementation directory: '+str(path.relative_to(root)))
    for path in prod:
        text = path.read_text('utf-8-sig')
        if re.search(r'^\s*(<{7}|>{7})', text, re.M): errors.append('Merge conflict marker: '+str(path.relative_to(root)))
        if 'namespace MiKiNuo.Mvi.V2' in text or 'IntentStoreAdapter' in text: errors.append('Parallel namespace/compatibility adapter: '+str(path.relative_to(root)))
        if re.search(r'\b(class|interface)\s+(?:MviEffectDispatcherBase|IMviEffectDispatcher|MviReduceResult)\b',text): errors.append('Retired execution type: '+str(path.relative_to(root)))
    storefiles=[p for p in prod if re.search(r'public\s+sealed\s+class\s+MviStore\s*<',p.read_text('utf-8-sig'))]
    if len(storefiles)!=1: errors.append('Must contain exactly one MviStore implementation.')
    if storefiles and not re.search(r'class MviStore<TState, TIntent>',storefiles[0].read_text('utf-8-sig')): errors.append('Store has the wrong contract.')
    vm=root/'sample/MiKiNuo.Mvi.Samples.Avalonia/Features/Login/LoginViewModel.cs'
    vmtext=vm.read_text('utf-8-sig')
    if re.search(r'\bLoginViewModel\s*\(|\b(?:SubmitAsync|EditAsync|NavigateAsync)\s*\(',vmtext): errors.append('Login ViewModel contains a handwritten constructor or forwarding method.')
    if len(re.findall(r'\[MviBind(?:\]|\()',vmtext))!=2 or len(re.findall(r'\[MviCommand\(',vmtext))!=3: errors.append('Unexpected login binding contract.')
    state=root/'sample/MiKiNuo.Mvi.Samples.Avalonia/Features/Login/LoginState.cs'
    if re.search(r'\bPassword\b',state.read_text('utf-8-sig')): errors.append('Password must not be in LoginState.')
    for path in root.rglob('*.axaml'): ET.parse(path)
    for path in list(root.glob('*.props'))+list(root.glob('*.targets')): ET.parse(path)
    client = root/'sample/MiKiNuo.Mvi.Samples.Avalonia/Features/Auth/HttpAuthService.cs'
    client_text = client.read_text('utf-8-sig')
    if 'https://dummyjson.com/' not in client_text or '127.0.0.1' in client_text or 'localhost' in client_text:
        errors.append('The sample client must use the third-party test service, never a local server.')
    for endpoint in ('auth/login', 'users/add', 'users/1'):
        if '"'+endpoint+'"' not in client_text:
            errors.append('Missing public test endpoint: '+endpoint)
    for path in (root/'sample').rglob('*.cs'):
        text = path.read_text('utf-8-sig')
        if re.search(r'\b(?:DemoAccounts|AuthReply|RecoveryRequest|ResetCode|ForgotPasswordAsync)\b', text):
            errors.append('Retired local authentication protocol: '+str(path.relative_to(root)))
    # IDs referenced by rewritten generators must exist in the catalogue.
    catalogue=(root/'src/MiKiNuo.Mvi.Generators/Diagnostics/DiagnosticIdCatalog.cs').read_text('utf-8-sig')
    ids=set(re.findall(r'public const string (\w+) =',catalogue))
    for path in (root/'src/MiKiNuo.Mvi.Generators').rglob('*.cs'):
        if path.name=='DiagnosticIdCatalog.cs': continue
        unknown=set(re.findall(r'DiagnosticIdCatalog\.(\w+)',path.read_text('utf-8-sig')))-ids-{'AllIds'}
        if unknown: errors.append(f'Unknown diagnostic constants in {path.name}: {sorted(unknown)}')
    return {'check':'source layout and reference integrity only','csharp_compiled':False,'passed':not errors,'errors':errors,
            'projects':len(projects),'project_references':sum(map(len,graph.values())),
            'csharp_files':len(sources),'xaml_files':len(list(root.rglob('*.axaml'))),
            'tunit_test_declarations':sum(len(re.findall(r'\[Test\]',p.read_text('utf-8-sig'))) for p in (root/'test/MiKiNuo.Mvi.Tests').rglob('*.cs')),
            'store_implementations':len(storefiles),'login_viewmodel_lines':len(vmtext.splitlines()),'project_graph':graph}

def main() -> int:
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root',type=Path,default=Path(__file__).resolve().parents[1]);parser.add_argument('--output',type=Path)
    args=parser.parse_args()
    try: result=verify(args.root.resolve())
    except (OSError,ET.ParseError,ValueError) as error: result={'passed':False,'errors':[str(error)],'csharp_compiled':False}
    text=json.dumps(result,ensure_ascii=False,indent=2)
    print(text)
    if args.output: args.output.parent.mkdir(parents=True,exist_ok=True);args.output.write_text(text+'\n',encoding='utf-8')
    return 0 if result['passed'] else 1
if __name__=='__main__': raise SystemExit(main())
