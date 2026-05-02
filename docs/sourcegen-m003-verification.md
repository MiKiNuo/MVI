# M003 SourceGen verification report

## Result

M003 final SourceGen verification passed.

Command run from the repository root:

```bash
dotnet test
```

Observed result:

- Total tests: 95
- Succeeded: 95
- Failed: 0
- Skipped: 0
- Test assemblies: analyzer tests, runtime core tests, and SourceGen tests

## R008 coverage

R008 required MVI Source Generators for IntentId, reducer dispatchers, handler dispatchers, ViewModel binding members, registries, and middleware pipelines.

M003 implemented and tested:

- SourceGen project and TUnit test project.
- SourceGen attribute contracts in Abstractions.
- `SourceGeneratorVerifier` that runs incremental generators and compiles generated code.
- `MviIntentKindGenerator` for generated `Kind` members.
- `MviIntentDispatcherGenerator` for generated handler dispatcher source.
- `MviViewModelBindingGenerator` for generated constructor, state bindings, StateFirst two-way setters, commands, and `ApplyStateCore`.
- `MviMediatorRouteGenerator` for generated request/response mediator route shape.
- `MviViewRegistryGenerator` for explicit reflection-free View to ViewModel type maps.
- `MviMiddlewarePipelineGenerator` for ordered middleware pipeline factory source.

## Design constraints checked

- Root `dotnet test` passes.
- Generated-code tests compile generated output through Roslyn.
- Mediator generator tests reject Publish/Subscribe/Broadcast/EventBus terms.
- View registry generator tests reject Assembly/GetTypes/Activator scanning.
- SourceGen production project targets `netstandard2.0`.
- No automatic commit was created; `git status --short` shows local changes only.

## Known limitations

- Generated DI container is not implemented in M003; that remains R009/M004.
- Avalonia sample application is not implemented in M003; that remains R010/M005.
- Generated dispatcher and ViewModel code are compile/source-shape tested, not executed from an in-memory compiled assembly.
- Duplicate route/kind/order diagnostics are not implemented.
- Nested types and global namespace generation are not fully supported.
- Mediator route generation emits route checks but not concrete handler invocation.
- Parameterized commands and `[MviCommandArgument]` mapping are not implemented.
- `MviTwoWayUpdateMode.Optimistic` is not implemented.
