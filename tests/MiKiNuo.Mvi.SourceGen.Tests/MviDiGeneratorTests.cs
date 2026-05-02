using MiKiNuo.Mvi.SourceGen.DependencyInjection;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.SourceGen.Tests;

public sealed class MviDiGeneratorTests
{
    [Test]
    public async Task Generator_emits_transient_resolve_method_without_reflection_scanning()
    {
        const string source = """
            using MiKiNuo.Mvi.Abstractions.DependencyInjection;

            namespace Demo;

            public interface IGreeter;

            [MviService(typeof(IGreeter), MviServiceLifetime.Transient)]
            public sealed class Greeter : IGreeter
            {
            }
            """;

        var result = await SourceGeneratorVerifier.VerifyAsync(
            source,
            new MviDiGenerator());

        await Assert.That(result.Diagnostics).IsEmpty();
        await Assert.That(result.GeneratedTrees.Select(tree => tree.FilePath)).Contains(filePath => filePath.EndsWith("GeneratedAppContainer.g.cs", StringComparison.Ordinal));
        await Assert.That(result.GeneratedTrees.Select(tree => tree.FilePath)).Contains(filePath => filePath.EndsWith("GeneratedMviScope.g.cs", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("public sealed partial class GeneratedAppContainer", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("public IGreeter ResolveIGreeter()", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("return new Greeter();", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).DoesNotContain(sourceText =>
            sourceText.Contains("Assembly", StringComparison.Ordinal) ||
            sourceText.Contains("GetTypes", StringComparison.Ordinal) ||
            sourceText.Contains("Activator", StringComparison.Ordinal));
    }

    [Test]
    public async Task Generator_emits_transient_constructor_dependency_resolution()
    {
        const string source = """
            using MiKiNuo.Mvi.Abstractions.DependencyInjection;

            namespace Demo;

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
            """;

        var result = await SourceGeneratorVerifier.VerifyAsync(
            source,
            new MviDiGenerator());

        await Assert.That(result.Diagnostics).IsEmpty();
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("public IGreeter ResolveIGreeter()", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("return new Greeter(ResolveIRepository());", StringComparison.Ordinal));
    }

    [Test]
    public async Task Generator_emits_singleton_cache_at_container_level()
    {
        const string source = """
            using MiKiNuo.Mvi.Abstractions.DependencyInjection;

            namespace Demo;

            public interface IClock;

            [MviService(typeof(IClock), MviServiceLifetime.Singleton)]
            public sealed class SystemClock : IClock
            {
            }
            """;

        var result = await SourceGeneratorVerifier.VerifyAsync(
            source,
            new MviDiGenerator());

        await Assert.That(result.Diagnostics).IsEmpty();
        var generatedSource = string.Join("\n", result.GeneratedSources);
        await Assert.That(generatedSource).Contains("private IClock? singletonIClock;", StringComparison.Ordinal);
        await Assert.That(generatedSource).Contains("internal IClock ResolveSingletonIClock()", StringComparison.Ordinal);
        await Assert.That(generatedSource).Contains("singletonIClock ??= new SystemClock();", StringComparison.Ordinal);
        await Assert.That(generatedSource).Contains("public IClock ResolveIClock()", StringComparison.Ordinal);
        await Assert.That(generatedSource).Contains("return container.ResolveSingletonIClock();", StringComparison.Ordinal);
        await Assert.That(generatedSource).DoesNotContain("return new SystemClock();", StringComparison.Ordinal);
    }

    [Test]
    public async Task Generator_emits_scoped_cache_at_scope_level()
    {
        const string source = """
            using MiKiNuo.Mvi.Abstractions.DependencyInjection;

            namespace Demo;

            public interface IRequestContext;

            [MviService(typeof(IRequestContext), MviServiceLifetime.Scoped)]
            public sealed class RequestContext : IRequestContext
            {
            }
            """;

        var result = await SourceGeneratorVerifier.VerifyAsync(
            source,
            new MviDiGenerator());

        await Assert.That(result.Diagnostics).IsEmpty();
        var generatedSource = string.Join("\n", result.GeneratedSources);
        await Assert.That(generatedSource).Contains("private IRequestContext? scopedIRequestContext;", StringComparison.Ordinal);
        await Assert.That(generatedSource).Contains("return scopedIRequestContext ??= new RequestContext();", StringComparison.Ordinal);
        await Assert.That(generatedSource).DoesNotContain("private IRequestContext? singletonIRequestContext;", StringComparison.Ordinal);
        await Assert.That(generatedSource).DoesNotContain("ResolveSingletonIRequestContext", StringComparison.Ordinal);
    }

    [Test]
    public async Task Generator_emits_disposal_paths_for_cached_lifetimes_only()
    {
        const string source = """
            using MiKiNuo.Mvi.Abstractions.DependencyInjection;

            namespace Demo;

            public interface ISingletonService;

            [MviService(typeof(ISingletonService), MviServiceLifetime.Singleton)]
            public sealed class SingletonService : ISingletonService
            {
            }

            public interface IScopedService;

            [MviService(typeof(IScopedService), MviServiceLifetime.Scoped)]
            public sealed class ScopedService : IScopedService
            {
            }

            public interface ITransientService;

            [MviService(typeof(ITransientService), MviServiceLifetime.Transient)]
            public sealed class TransientService : ITransientService
            {
            }
            """;

        var result = await SourceGeneratorVerifier.VerifyAsync(
            source,
            new MviDiGenerator());

        await Assert.That(result.Diagnostics).IsEmpty();
        var generatedSource = string.Join("\n", result.GeneratedSources);
        await Assert.That(generatedSource).Contains("return MviGeneratedDisposal.DisposeAsync(new object?[] { singletonISingletonService });", StringComparison.Ordinal);
        await Assert.That(generatedSource).Contains("return MviGeneratedDisposal.DisposeAsync(new object?[] { scopedIScopedService });", StringComparison.Ordinal);
        await Assert.That(generatedSource).DoesNotContain("transientITransientService", StringComparison.Ordinal);
        await Assert.That(generatedSource).DoesNotContain("ITransientService?", StringComparison.Ordinal);
    }

    [Test]
    public async Task Generator_reports_duplicate_service_registration()
    {
        const string source = """
            using MiKiNuo.Mvi.Abstractions.DependencyInjection;

            namespace Demo;

            public interface IGreeter;

            [MviService(typeof(IGreeter), MviServiceLifetime.Transient)]
            public sealed class MorningGreeter : IGreeter
            {
            }

            [MviService(typeof(IGreeter), MviServiceLifetime.Transient)]
            public sealed class EveningGreeter : IGreeter
            {
            }
            """;

        var result = await SourceGeneratorVerifier.VerifyAsync(
            source,
            new MviDiGenerator());

        await Assert.That(result.Diagnostics).Contains(diagnostic => diagnostic.Id == "MVIDI0002");
        await Assert.That(result.Diagnostics).Contains(diagnostic => diagnostic.GetMessage().Contains("IGreeter", StringComparison.Ordinal));
    }

    [Test]
    public async Task Generator_reports_ambiguous_public_constructors()
    {
        const string source = """
            using MiKiNuo.Mvi.Abstractions.DependencyInjection;

            namespace Demo;

            public interface IRepository;

            [MviService(typeof(IRepository), MviServiceLifetime.Transient)]
            public sealed class Repository : IRepository
            {
            }

            public interface IClock;

            [MviService(typeof(IClock), MviServiceLifetime.Transient)]
            public sealed class Clock : IClock
            {
            }

            public interface IGreeter;

            [MviService(typeof(IGreeter), MviServiceLifetime.Transient)]
            public sealed class Greeter : IGreeter
            {
                public Greeter(IRepository repository)
                {
                }

                public Greeter(IClock clock)
                {
                }
            }
            """;

        var result = await SourceGeneratorVerifier.VerifyAsync(
            source,
            new MviDiGenerator());

        await Assert.That(result.Diagnostics).Contains(diagnostic => diagnostic.Id == "MVIDI0003");
        await Assert.That(result.Diagnostics).Contains(diagnostic => diagnostic.GetMessage().Contains("Greeter", StringComparison.Ordinal));
    }

    [Test]
    public async Task Generator_reports_circular_constructor_dependency()
    {
        const string source = """
            using MiKiNuo.Mvi.Abstractions.DependencyInjection;

            namespace Demo;

            public interface IServiceA;
            public interface IServiceB;

            [MviService(typeof(IServiceA), MviServiceLifetime.Transient)]
            public sealed class ServiceA : IServiceA
            {
                public ServiceA(IServiceB serviceB)
                {
                }
            }

            [MviService(typeof(IServiceB), MviServiceLifetime.Transient)]
            public sealed class ServiceB : IServiceB
            {
                public ServiceB(IServiceA serviceA)
                {
                }
            }
            """;

        var result = await SourceGeneratorVerifier.VerifyAsync(
            source,
            new MviDiGenerator());

        await Assert.That(result.Diagnostics).Contains(diagnostic => diagnostic.Id == "MVIDI0004");
        await Assert.That(result.Diagnostics).Contains(diagnostic => diagnostic.GetMessage().Contains("IServiceA -> IServiceB -> IServiceA", StringComparison.Ordinal));
    }

    [Test]
    public async Task Generator_reports_unresolved_constructor_dependency()
    {
        const string source = """
            using MiKiNuo.Mvi.Abstractions.DependencyInjection;

            namespace Demo;

            public interface IExternalDependency;

            public interface IGreeter;

            [MviService(typeof(IGreeter), MviServiceLifetime.Transient)]
            public sealed class Greeter : IGreeter
            {
                public Greeter(IExternalDependency dependency)
                {
                }
            }
            """;

        var result = await SourceGeneratorVerifier.VerifyAsync(
            source,
            new MviDiGenerator());

        await Assert.That(result.Diagnostics).Contains(diagnostic => diagnostic.Id == "MVIDI0001");
        await Assert.That(result.Diagnostics).Contains(diagnostic => diagnostic.GetMessage().Contains("IExternalDependency", StringComparison.Ordinal));
        await Assert.That(string.Join("\n", result.GeneratedSources)).DoesNotContain("default(", StringComparison.Ordinal);
    }

    [Test]
    public async Task Generator_emits_multiple_transient_resolve_methods_deterministically()
    {
        const string source = """
            using MiKiNuo.Mvi.Abstractions.DependencyInjection;

            namespace Demo;

            public interface IGreeter;

            [MviService(typeof(IGreeter), MviServiceLifetime.Transient)]
            public sealed class Greeter : IGreeter
            {
            }

            public interface IClock;

            [MviService(typeof(IClock), MviServiceLifetime.Transient)]
            public sealed class SystemClock : IClock
            {
            }
            """;

        var result = await SourceGeneratorVerifier.VerifyAsync(
            source,
            new MviDiGenerator());

        await Assert.That(result.Diagnostics).IsEmpty();
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("public IClock ResolveIClock()", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("return new SystemClock();", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("public IGreeter ResolveIGreeter()", StringComparison.Ordinal));
        await Assert.That(result.GeneratedSources).Contains(sourceText => sourceText.Contains("return new Greeter();", StringComparison.Ordinal));
        var generatedSource = string.Join("\n", result.GeneratedSources);
        await Assert.That(generatedSource).Contains("ResolveIClock", StringComparison.Ordinal);
        await Assert.That(generatedSource).Contains("ResolveIGreeter", StringComparison.Ordinal);
        await Assert.That(generatedSource.IndexOf("ResolveIClock", StringComparison.Ordinal)).IsLessThan(generatedSource.IndexOf("ResolveIGreeter", StringComparison.Ordinal));
    }
}

