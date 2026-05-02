# M004 Generated DI

## Purpose

M004 adds the first generated dependency injection container for MiKiNuo MVI. It is intentionally narrow: it generates explicit container and scope code from compile-time service registrations, without runtime reflection scanning or a general-purpose DI abstraction.

After reading this document, a contributor should be able to register a service, predict the generated lifetime behavior, and understand generator diagnostics when the service graph is invalid.

## Registration contract

Services are registered with `MviServiceAttribute` from `MiKiNuo.Mvi.Abstractions.DependencyInjection`.

```csharp
using MiKiNuo.Mvi.Abstractions.DependencyInjection;

public interface IGreeter;

[MviService(typeof(IGreeter), MviServiceLifetime.Transient)]
public sealed class Greeter : IGreeter
{
}
```

The attribute is applied to implementation classes. The first argument is the service abstraction. The second argument is the lifetime:

- `Transient` creates a new implementation each time the service is resolved.
- `Scoped` reuses one implementation inside one generated scope.
- `Singleton` reuses one implementation owned by the generated container.

A class may carry multiple `MviServiceAttribute` instances, but each service abstraction may have only one effective registration in a generated graph.

## Generated container and scope

The generator emits two partial types in the service namespace:

- `GeneratedAppContainer`, which implements `IMviServiceContainer`.
- `GeneratedMviScope`, which implements `IMviServiceScope`.

`GeneratedAppContainer` creates scopes and owns singleton cache fields. `GeneratedMviScope` owns scoped cache fields and exposes deterministic `Resolve{ServiceName}` methods.

For a service abstraction named `IGreeter`, the generated scope exposes:

```csharp
public IGreeter ResolveIGreeter()
```

Generated resolve methods construct implementations explicitly. They do not call `Assembly`, `GetTypes`, or `Activator`.

## Constructor injection

The generator selects the unique public constructor with the highest parameter count. Constructor parameters must be registered service abstractions in the same generated graph.

Example:

```csharp
public interface IRepository;

[MviService(typeof(IRepository), MviServiceLifetime.Transient)]
public sealed class Repository : IRepository
{
}

public interface IGreeter;

[MviService(typeof(IGreeter), MviServiceLifetime.Transient)]
public sealed class Greeter : IGreeter
{
    public Greeter(IRepository repository)
    {
    }
}
```

The generated `ResolveIGreeter` method constructs `Greeter` by calling `ResolveIRepository()`.

## Lifetime behavior

### Transient

Transient services are not cached. Every resolve method returns a new implementation instance.

### Scoped

Scoped services are cached on `GeneratedMviScope`. Multiple calls to the same scoped resolve method on one scope reuse the same instance. A new scope has a new scoped cache.

### Singleton

Singleton services are cached on `GeneratedAppContainer`. Scope resolve methods delegate singleton resolution back to the container. Different scopes created by the same container share singleton instances.

## Disposal behavior

Both generated container and generated scope implement async disposal.

Disposal is explicit and cache-based:

- Container disposal passes cached singleton fields to `MviGeneratedDisposal.DisposeAsync`.
- Scope disposal passes cached scoped fields to `MviGeneratedDisposal.DisposeAsync`.
- Transient services are not cached and are not included in generated disposal lists.

`MviGeneratedDisposal` disposes async-disposable instances before disposable instances and ignores null entries.

Current disposal limitations:

- Generated disposal does not clear cache fields after disposal.
- Generated disposal does not add an explicit repeated-dispose policy.
- Exception aggregation is not implemented.

## Diagnostics

Invalid generated DI graphs fail during source generation with stable diagnostics instead of emitting unsafe fallback source.

| ID | Meaning | Typical fix |
| --- | --- | --- |
| `MVIDI0001` | A registered service constructor depends on an unregistered service. | Add a `MviServiceAttribute` registration for the missing abstraction, or remove the dependency from the generated graph. |
| `MVIDI0002` | More than one implementation registers the same service abstraction. | Keep one registration for the abstraction, or split the abstractions. |
| `MVIDI0003` | An implementation has multiple public constructors tied for the selected parameter count. | Leave one public constructor as the generated DI constructor. |
| `MVIDI0004` | Registered services contain a constructor dependency cycle. | Break the cycle by introducing a different boundary, factory, mediator, or effect path. |

When one of these diagnostics is reported, the invalid namespace group is not rendered. This avoids partial generated code that would hide the graph problem.

## Determinism and no-scanning rule

Generated services are ordered deterministically by resolve method suffix. This keeps generated output stable when source declaration order changes.

The generated DI path preserves the MVI architecture rule that framework composition is compile-time explicit. Generated source is tested to avoid runtime scanning terms such as `Assembly`, `GetTypes`, and `Activator`.

## Relationship to earlier milestones

M002 provides the runtime contracts consumed by generated DI: async-disposable container/scope interfaces and the generated disposal helper.

M003 provides the SourceGen test harness and earlier generators. M004 extends that SourceGen layer with generated composition primitives.

## Current limits

M004 does not implement:

- Avalonia, WinForms, Godot, Unity, or sample application integration.
- Package publishing.
- General-purpose DI features such as open generics, keyed services, factory delegates, optional dependencies, or service collections.
- Explicit constructor-selection attributes.
- Nested type and global namespace hardening beyond the tested top-level named-namespace scenarios.
- Runtime execution of generated assemblies from an in-memory compiled output.

Those are future milestones or future hardening work. M004 proves the generated DI container shape, lifetime ownership, disposal paths, and graph diagnostics needed before a sample application can be wired safely.
