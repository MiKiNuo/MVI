# M005 Avalonia Sample Scaffold

## Purpose

S01 establishes the build and test baseline for the M005 Avalonia sample. It does not implement login, dashboard navigation, mediator communication, or generated composition behavior. Those are later M005 slices.

## Projects

The sample baseline contains:

- `MiKiNuo.Mvi.Samples.Avalonia` — a minimal Avalonia desktop app.
- `MiKiNuo.Mvi.Samples.Avalonia.Tests` — a TUnit test project for non-UI sample behavior tests.

The sample app references the MVI Abstractions and Core projects. It currently exposes a minimal `App`, `Program`, and `MainWindow` so restore/build compatibility is proven before feature code is added.

## Package baseline

Avalonia package versions are centrally managed:

- `Avalonia` 12.0.2
- `Avalonia.Desktop` 12.0.2
- `Avalonia.Themes.Fluent` 12.0.2

These versions restored and built successfully under the repo's .NET 10 SDK setup.

## Verification commands

Build just the sample app:

```bash
dotnet build sample/MiKiNuo.Mvi.Samples.Avalonia/MiKiNuo.Mvi.Samples.Avalonia.csproj
```

Run just the sample tests:

```bash
dotnet test --project tests/MiKiNuo.Mvi.Samples.Avalonia.Tests/MiKiNuo.Mvi.Samples.Avalonia.Tests.csproj
```

Run full repository verification:

```bash
dotnet test
```

At S01/T02 closeout, root verification passed with 111 total tests, 111 succeeded, and 0 failed.

## Current intentional limits

- The main window only displays scaffold text.
- No login state, intent, handler, or ViewModel exists yet.
- No dashboard components exist yet.
- No sample mediator route exists yet.
- No sample generated DI consumption exists yet.
- No desktop UI automation is part of S01.

These limits are intentional. S01 proves package/tooling compatibility and establishes a test harness. S02 starts the login feature behavior.
