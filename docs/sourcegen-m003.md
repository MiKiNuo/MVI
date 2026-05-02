# M003 SourceGen behavior

## Purpose

M003 implements the first compile-time SourceGen layer for MiKiNuo MVI. It consumes the analyzer/runtime foundations from M001 and M002 and generates repetitive MVI infrastructure without runtime reflection scanning.

M003 does not implement the generated DI container, platform adapters, or sample applications.

## Verification

Run from the repository root:

```bash
dotnet test
```

SourceGen tests use `SourceGeneratorVerifier`, which runs Roslyn generators and compiles generated code. Generated-source string assertions are paired with compilation diagnostics so tests do not rely on snapshots alone.

## Attribute contracts

Attributes live in `MiKiNuo.Mvi.Abstractions` so user projects can reference generator inputs without referencing the SourceGen assembly directly.

### Binding attributes

- `MviViewModelAttribute`
- `MviStateBindingAttribute`
- `MviTwoWayBindingAttribute`
- `MviTwoWayUpdateMode`
- `MviCommandAttribute`
- `MviCommandArgumentAttribute`

### Generation attributes

- `MviIntentUnionAttribute`
- `MviIntentKindAttribute`
- `MviIntentHandlerAttribute`
- `MviMediatorAttribute`
- `MviRouteAttribute`
- `MviViewRegistryAttribute`
- `MviMiddlewareAttribute`

## Implemented generators

### `MviIntentKindGenerator`

Input:

- `[MviIntentUnion]` on an abstract partial intent base.
- `[MviIntentKind(n)]` on concrete partial intent types.

Output:

- Abstract `Kind` property on the union base.
- Override `Kind` property on concrete intent types.
- Auto-generated header and Chinese XML summary.

Current limits:

- Assumes top-level types in named namespaces.
- No duplicate kind diagnostics yet.
- No nested/global namespace support yet.

### `MviIntentDispatcherGenerator`

Input:

- `[MviIntentUnion]` intent base.
- `[MviIntentKind]` concrete intents.
- `[MviIntentHandler(typeof(IntentType))]` handlers implementing `IIntentHandler<TState,TIntent,TEffect>`.

Output:

- `{IntentUnionName}Dispatcher` implementing `IIntentDispatcher<TState,TIntentUnion,TEffect>`.
- Constructor-injected handlers.
- `DispatchAsync` switch over `intent.Kind`.
- `InvalidOperationException` for unregistered intent kinds.

Current limits:

- Compile/source-shape tested; generated dispatcher is not executed from an in-memory compiled assembly.
- No duplicate kind or missing handler diagnostics yet.
- Same namespace/top-level scenarios only.

### `MviViewModelBindingGenerator`

Input:

- `[MviViewModel]` partial class inheriting `MviViewModelBase<TState,TIntent,TEffect>`.
- `[MviStateBinding]` partial properties.
- `[MviTwoWayBinding]` partial properties.
- `[MviCommand]` partial `ICommand` properties.

Output:

- Constructor accepting `IMviStore<TState,TIntent,TEffect>` and calling `base(store)`.
- Backing fields for state-bound properties.
- Partial property implementations.
- `ApplyStateCore` that updates generated backing fields from state.
- State update helpers with equality guard, `RaisePropertyChanged`, and command can-execute notification.
- StateFirst two-way setters that dispatch `new Intent(value)` without mutating the backing field directly.
- `MviIntentCommand<TViewModel,TIntent>` fields and `ICommand` properties.

Current limits:

- Parameterized commands and `[MviCommandArgument]` mapping are not implemented.
- `MviTwoWayUpdateMode.Optimistic` is not implemented.
- No diagnostics for invalid declarations or constructor/member collisions yet.
- Generated ViewModel code is compile/source-shape tested, not runtime-instantiated.

### `MviMediatorRouteGenerator`

Input:

- `[MviMediator]` partial class implementing `IMviMediator`.
- `[MviRoute(typeof(TRequest), typeof(TResponse))]` route attributes.

Output:

- `SendAsync<TRequest,TResponse>` request/response route table shape.
- Typed request/response route checks.
- No `Publish`, `Subscribe`, `Broadcast`, or `EventBus` semantics.

Current limits:

- Route checks compile but concrete route handler invocation is not implemented yet.
- Handler invocation should be designed with generated DI or an explicit mediator handler contract.

### `MviViewRegistryGenerator`

Input:

- `[MviViewRegistry(typeof(View), typeof(ViewModel))]` attributes on a partial registry class.

Output:

- `TryGetViewModelType(Type viewType, out Type? viewModelType)`.
- Explicit `typeof(View)` to `typeof(ViewModel)` mappings.
- No `Assembly`, `GetTypes`, or `Activator` scanning.

Current limits:

- Registry maps types only; it does not construct views or ViewModels.
- Construction belongs to generated DI or platform sample milestones.

### `MviMiddlewarePipelineGenerator`

Input:

- `[MviMiddleware(order)]` middleware classes implementing `IMviMiddleware<TState,TIntent,TEffect>`.

Output:

- `{FeatureName}MiddlewarePipelineFactory` grouped by state/intent/effect tuple.
- Factory accepting `IIntentDispatcher<TState,TIntent,TEffect>` and ordered middleware instances.
- `MviMiddlewarePipeline<TState,TIntent,TEffect>` construction using M002 runtime contracts.

Current limits:

- Factory name derives from the intent type name by trimming `Intent`.
- Duplicate order diagnostics are not implemented.
- No generated DI integration yet.

## Design constraints preserved

- No runtime reflection scanning.
- No ReactiveUI or System.Reactive runtime dependency.
- Mediator generation remains request/response only.
- SourceGen assembly targets `netstandard2.0`; production generator internals avoid C# record types that require `IsExternalInit`.
- Tests target `net10.0` with TUnit and Microsoft.Testing.Platform.

## Handoff to later milestones

M004 should implement generated DI. It can consume the generated dispatcher, ViewModel constructors, mediator route shapes, registry maps, and middleware pipeline factories from M003.

M005 should prove the full flow in a visible sample after DI is in place.
