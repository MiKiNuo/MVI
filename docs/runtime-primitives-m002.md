# M002 runtime primitives

## Purpose

M002 implements the core MVI runtime primitives that later SourceGen, DI, and sample milestones can build on. It does not implement source generators, generated DI, platform adapters, or a sample application.

## Verification

Run from the repository root:

```bash
dotnet test
```

The root suite runs both analyzer tests and runtime tests.

## Projects

- `MiKiNuo.Mvi.Abstractions` contains marker contracts shared by runtime, analyzers, and future generators.
- `MiKiNuo.Mvi.Core` contains Store, reducers, middleware, effects, ViewModel primitives, commands, and mediator contracts.
- `MiKiNuo.Mvi.Core.Tests` contains TUnit behavior tests for runtime primitives.

## Implemented primitives

### Abstractions

- `IMviState` marks immutable UI state snapshots.
- `IMviIntent` marks user/system intents and exposes `Kind` for future generated dispatchers.
- `IMviEffect` marks side-effect descriptions.

### Reducers

- `ReduceResult<TState, TEffect>` carries a reduced state plus effects to emit.
- `ReduceResults.StateOnly` creates a state-only result.
- `ReduceResults.WithEffect` creates a single-effect result.
- `ReduceResults.WithEffects` creates a multi-effect result and snapshots the effect list.

### Store and dispatch

- `IMviStore<TState,TIntent,TEffect>` exposes R3 `Observable<TState>` and `Observable<TEffect>` streams and `DispatchAsync`.
- `MviStore<TState,TIntent,TEffect>` keeps current state, dispatches intents, publishes reduced state, then publishes effects.
- `IIntentDispatcher<TState,TIntent,TEffect>` represents generated or test dispatcher logic.
- `IIntentHandler<TState,TIntent,TEffect>` represents one intent handler contract.

### Middleware

- `MviMiddlewareContext<TState,TIntent,TEffect>` carries current state and intent.
- `MviContinuation<TState,TIntent,TEffect>` continues the dispatch pipeline.
- `IMviMiddleware<TState,TIntent,TEffect>` wraps dispatch behavior.
- `MviMiddlewarePipeline<TState,TIntent,TEffect>` executes middleware sequentially and then calls the dispatcher.

Middleware uses the parameter name `continuation`, not `next`, to avoid the CA1716 keyword concern called out by the architecture document.

### Effects

- `IEffectHandler<TEffect,TIntent>` handles one effect type.
- `IIntentSink<TIntent>` lets an effect handler dispatch follow-up intents explicitly.
- `NullIntentSink<TIntent>` ignores follow-up intents.
- `StoreIntentSink<TState,TIntent,TEffect>` dispatches follow-up intents back through `IMviStore`.
- `MviEffectRuntime<TIntent>` routes effects to explicitly registered handlers.

Unhandled effects are ignored. Handler exceptions propagate to the caller of `HandleAsync`.

### ViewModel and commands

- `MviObservableObject` implements `INotifyPropertyChanged` and protected `RaisePropertyChanged`.
- `MviViewModelBase<TState,TIntent,TEffect>` subscribes to Store state, calls `ApplyStateCore`, and exposes protected `DispatchAsync` for generated or derived ViewModel code.
- `MviIntentCommand<TViewModel,TIntent>` implements `System.Windows.Input.ICommand`, invokes a ViewModel execute delegate, honors an optional can-execute delegate, and exposes `RaiseCanExecuteChanged`.

These primitives do not depend on Avalonia, WinForms, Godot, Unity, or any UI framework.

### Mediator contracts

- `ComponentAddress` identifies a target component. `ComponentAddress.Root` is the stable root address.
- `IMviMediator.SendAsync<TRequest,TResponse>` is the request/response mediator contract.

No event-bus APIs such as Publish, Subscribe, Broadcast, On, EventBus, or MessageBus were introduced.

## Dependency choices

- R3 is used for Store state and effect streams, matching the architecture document.
- Runtime projects target `netstandard2.0` for broad compatibility.
- Runtime tests target `net10.0` and use TUnit through Microsoft.Testing.Platform.
- `IsExternalInit` compatibility is provided internally for record syntax on `netstandard2.0`.

## Known limits

- Store dispatch is not serialized for concurrent dispatch calls yet.
- Built-in logging, performance, exception, cancellation, authorization, and scheduler middleware are not implemented.
- Effect runtime invokes the first matching handler only; fan-out and retry policies are not defined.
- `MviIntentCommand.Execute` starts the provided async delegate from the void `ICommand.Execute` method; async exception to ErrorEffect conversion is future work.
- ViewModel base does not marshal state application onto a UI scheduler.
- Source-generated ViewModel bindings, generated dispatchers, generated DI, and sample applications are deferred to later milestones.
