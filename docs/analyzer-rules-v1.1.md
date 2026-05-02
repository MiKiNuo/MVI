# Analyzer rules for architecture document v1.1

## Reader and action

This document is for a future contributor or agent landing cold in the analyzer milestone. After reading it, they should be able to decide whether a diagnostic belongs to the implemented M001 analyzer set or a deferred v1.1 rule family, and they should know how to verify the current analyzer behavior.

## Current verification command

Run the analyzer suite from the repository root:

```bash
dotnet test
```

The test project uses TUnit on Microsoft.Testing.Platform. Use the plain command above for milestone verification. Do not use VSTest-only options such as `--filter` or `--nologo` unless the runner configuration is deliberately changed.

## Implemented rule catalog

M001 implements four diagnostics:

| Diagnostic ID | Severity | Category | Implemented rule | v1.1 alignment |
|---|---:|---|---|---|
| `MVI0001` | Error | Architecture | Clean Architecture reference violation | Matches v1.1 `MVI0001`. |
| `MVI0002` | Error | Architecture | Domain/Application references platform UI | Matches v1.1 `MVI0002`. |
| `MNK0006` | Error | Documentation | Effective-public API requires Chinese XML documentation | Legacy M001/user-requested ID. v1.1 reserves broader documentation enforcement under `MVI0007`. |
| `MNK0012` | Error | Naming | First Microsoft naming subset | Legacy M001/user-requested ID. It is intentionally not v1.1 `MVI0012`, which means View directly referencing another MVI component ViewModel. |

The mixed prefixes are intentional. M001 began before the v1.1 analyzer table was updated. Clean Architecture work moved to the v1.1 `MVI` IDs because it had not yet shipped in this milestone. The documentation and naming analyzers already had milestone tests and summaries under `MNK0006` and `MNK0012`, so S05 keeps those IDs stable and documents the compatibility boundary instead of silently renaming them.

## Rule behavior

### `MVI0001` — Clean Architecture reference violation

Reports when a resolved type reference crosses a forbidden v1.1 architecture boundary that is not covered by `MVI0002`.

Covered examples include:

- Domain referencing Infrastructure or DI Runtime.
- Application referencing Infrastructure implementations.
- Core referencing platform UI.
- Presentation referencing platform UI or concrete Infrastructure.
- Platform adapters referencing business Infrastructure details.
- SourceGen referencing business runtime implementation instead of Abstractions.

The analyzer uses semantic type references rather than unused namespace imports. It checks declaration surfaces where architecture leaks are explicit: fields, properties, ordinary method signatures, constructor signatures, base types, and implemented interfaces.

### `MVI0002` — Domain/Application references platform UI

Reports when Domain or Application references platform UI types such as Avalonia, WinForms, Godot, Unity, or MiKiNuo platform adapter namespaces.

This rule has a separate ID because v1.1 calls out Domain/Application platform UI references as a distinct diagnostic.

### `MNK0006` — Chinese XML documentation

Reports when required effective-public API symbols are missing XML documentation or the XML documentation contains no common CJK character.

Covered symbols in M001:

- class
- interface
- property
- ordinary method
- field
- constant

The rule is intentionally conservative. It checks effective public API only: the symbol and all containing types must be public. This avoids false positives for internal implementation surfaces.

v1.1 describes a broader documentation rule for public/internal members and additional symbol kinds under `MVI0007`. That broader rule is deferred; do not expand `MNK0006` into `MVI0007` without a new planned slice.

### `MNK0012` — Microsoft naming subset

Reports when the first milestone naming subset violates the selected Microsoft-style conventions.

Covered conventions in M001:

- Classes use PascalCase.
- Interfaces use `I` followed by PascalCase.
- Properties use PascalCase.
- Ordinary methods use PascalCase.
- Regular fields use camelCase or `_camelCase`.
- Constants use PascalCase.

Out-of-scope symbols such as structs, delegates, events, enum members, constructors, accessors, acronym rules, async suffix guidance, and generic type parameters remain deferred.

Do not confuse `MNK0012` with v1.1 `MVI0012`. The latter is a future View composition rule, not a naming rule.

## Deferred v1.1 analyzer families

The v1.1 document names many analyzer rules that M001 does not implement. They remain future work unless a later milestone explicitly plans them:

- Reflection API bans.
- ReactiveUI/System.Reactive reference bans.
- View business-logic bans.
- ViewModel platform-control and business-service bans.
- Reducer/IntentHandler IO bans.
- Intent, Effect, and Mediator route registration checks.
- Source-generated duplicate type checks.
- ViewModel attribute and property-level binding rules.
- Project directory layout checks.
- Broader Chinese XML documentation enforcement under `MVI0007`.

## Build and test shape

The analyzer assembly targets an analyzer-host-compatible framework. The test assembly targets the current .NET SDK used by the repository and runs TUnit through Microsoft.Testing.Platform. Analyzer behavior tests compile in-memory C# fixtures with Roslyn and inspect analyzer diagnostics directly.

Root `dotnet test` is the authoritative local signal for this milestone. A passing run proves the documentation, naming, Clean Architecture, and catalog integration tests together.

## Known limits of the current Clean Architecture analyzer

The current Clean Architecture analyzer is a fixture-level semantic type-reference analyzer. It does not yet inspect:

- method bodies;
- local variables;
- object creation expressions;
- invocation targets;
- MSBuild project references;
- package references;
- ReactiveUI/System.Reactive usage.

Those limits are deliberate for M001. They should be treated as future hardening work, not as regressions in the current milestone.
