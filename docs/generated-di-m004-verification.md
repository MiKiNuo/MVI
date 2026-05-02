# M004 Generated DI Verification

## Command

Run from the repository root:

```bash
dotnet test
```

## Result

The final M004 verification run passed.

- Total: 110
- Succeeded: 110
- Failed: 0
- Skipped: 0
- Duration reported by Microsoft.Testing.Platform: 45s 924ms

Assemblies reported as passed:

- MiKiNuo.Mvi.Core.Tests
- MiKiNuo.Mvi.SourceGen.Tests
- MiKiNuo.Mvi.Analyzers.Tests

## Requirement outcome

R009 is validated by M004.

Proof:

- Generated DI registration contracts exist through `MviServiceAttribute` and `MviServiceLifetime`.
- Generated runtime contracts exist through `IMviServiceContainer`, `IMviServiceScope`, and `MviGeneratedDisposal`.
- `MviDiGenerator` emits compile-valid `GeneratedAppContainer` and `GeneratedMviScope` source.
- Transient services resolve through explicit construction.
- Scoped services cache on generated scopes.
- Singleton services cache on generated containers.
- Cached scoped and singleton services are passed to `MviGeneratedDisposal.DisposeAsync`.
- Generated source tests assert no runtime scanning terms such as `Assembly`, `GetTypes`, or `Activator`.
- Invalid graphs report stable diagnostics:
  - `MVIDI0001` unresolved constructor dependency
  - `MVIDI0002` duplicate service registration
  - `MVIDI0003` ambiguous public constructor selection
  - `MVIDI0004` circular constructor dependency

## Documentation produced

`docs/generated-di-m004.md` documents:

- Service registration attributes.
- Generated container and scope shape.
- Constructor injection behavior.
- Transient, scoped, and singleton lifetime behavior.
- Disposal behavior.
- Diagnostics `MVIDI0001` through `MVIDI0004`.
- Current limitations.

The generated DI document is linked from both README entry points.

## Known limitations after M004

M004 does not include sample application integration, package publishing, open generic/keyed/factory DI features, explicit constructor-selection attributes, nested/global namespace hardening, or runtime execution of generated assemblies from in-memory compilation.

These limitations do not block R009 because the requirement is specifically for a source-generated DI container foundation, and M004 proves the compile-time container/scope generation, lifetimes, disposal, and graph diagnostics needed by later sample work.
