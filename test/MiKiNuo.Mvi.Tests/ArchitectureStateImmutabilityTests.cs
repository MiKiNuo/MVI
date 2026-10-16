using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using MiKiNuo.Mvi.Generators.Analyzers;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>验证业务状态公开表面的不可变性诊断。</summary>
public sealed class ArchitectureStateImmutabilityTests
{
    /// <summary>公开 setter 可绕过 Mutation，必须报告诊断。</summary>
    [Test]
    public async Task PublicStateSetterProducesDiagnosticAsync()
    {
        const string source = """
            public sealed class CounterState : MiKiNuo.Mvi.Abstractions.MVI.State.IMviState
            {
                public int Count { get; set; }
            }
            """;
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source);
        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].Severity).IsEqualTo(DiagnosticSeverity.Warning);
        await Assert.That(diagnostics[0].Location.SourceTree!.GetText().ToString(diagnostics[0].Location.SourceSpan)).IsEqualTo("Count");
    }

    /// <summary>公开字段及只读属性暴露的常见可变集合均破坏状态快照。</summary>
    [Test]
    [Arguments("public int Value;")]
    [Arguments("public readonly int[] Value = [];")]
    [Arguments("public System.Collections.Generic.List<int> Value { get; init; } = [];")]
    [Arguments("public System.Collections.Generic.Dictionary<string, int> Value { get; } = [];")]
    [Arguments("public System.Collections.Generic.HashSet<int> Value { get; } = [];")]
    [Arguments("public System.Collections.Generic.Queue<int> Value { get; } = new();")]
    [Arguments("public System.Collections.Generic.Stack<int> Value { get; } = new();")]
    [Arguments("public System.Collections.ObjectModel.ObservableCollection<int> Value { get; } = [];")]
    [Arguments("public System.Collections.Concurrent.ConcurrentDictionary<string, int> Value { get; } = new();")]
    [Arguments("public System.Collections.Generic.ICollection<int> Value { get; init; } = new System.Collections.Generic.List<int>();")]
    [Arguments("public System.Collections.IList Value { get; } = new System.Collections.ArrayList();")]
    public async Task MutableMemberProducesOneDiagnosticAsync(string member)
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(
            "public sealed class State : MiKiNuo.Mvi.Abstractions.MVI.State.IMviState { " + member + " }");
        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].GetMessage().Contains("Value", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>位置记录与只读集合不因内部实现或私有成员被误报。</summary>
    [Test]
    public async Task ImmutableAndNonPublicMembersDoNotProduceDiagnosticsAsync()
    {
        const string source = """
            using System.Collections.Immutable;
            public sealed record State(int Count, ImmutableArray<int> Items)
                : MiKiNuo.Mvi.Abstractions.MVI.State.IMviState
            {
                public readonly int Value;
                public int Version { get; private set; }
                public ImmutableList<int> History { get; init; } = ImmutableList<int>.Empty;
                public System.Collections.ObjectModel.ReadOnlyCollection<int> View { get; } = new(new int[0]);
                public System.Collections.Generic.IReadOnlyList<int> Names { get; init; } = ImmutableArray<int>.Empty;
                private System.Collections.Generic.List<int> Cache { get; } = [];
                public static int Counter { get; set; }
            }
            public sealed class NotAState { public int Value { get; set; } }
            """;
        await Assert.That(await AnalyzeAsync(source)).IsEmpty();
    }

    /// <summary>记录的位置集合属性与集合子类不能规避诊断。</summary>
    [Test]
    public async Task PositionalAndDerivedCollectionsProduceDiagnosticsAsync()
    {
        const string source = """
            public sealed class Values : System.Collections.Generic.List<int> { }
            public sealed record State(Values Items)
                : MiKiNuo.Mvi.Abstractions.MVI.State.IMviState;
            """;
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source);
        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].Location.SourceTree!.GetText().ToString(diagnostics[0].Location.SourceSpan)).IsEqualTo("Items");
    }

    /// <summary>从非状态基类继承的公开可变成员在状态类型处报告。</summary>
    [Test]
    public async Task InheritedMutableMemberProducesDiagnosticAsync()
    {
        const string source = """
            public class Model { public int Count { get; set; } }
            public sealed class State : Model, MiKiNuo.Mvi.Abstractions.MVI.State.IMviState { }
            """;
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source);
        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].Location.SourceTree!.GetText().ToString(diagnostics[0].Location.SourceSpan)).IsEqualTo("State");
    }

    /// <summary>基状态已报告的成员不在每个派生状态重复报告。</summary>
    [Test]
    public async Task StateInheritanceDoesNotDuplicateDiagnosticsAsync()
    {
        const string source = """
            public class State : MiKiNuo.Mvi.Abstractions.MVI.State.IMviState { public int Count { get; set; } }
            public sealed class ChildState : State { }
            """;
        await Assert.That((await AnalyzeAsync(source)).Length).IsEqualTo(1);
    }

    /// <summary>泛型基状态闭合为可变集合后仍需报告，同时避免重复报告基类 setter。</summary>
    [Test]
    public async Task ClosedGenericStateReportsNewMutableSurfaceOnlyAsync()
    {
        const string source = """
            public class Base<T> : MiKiNuo.Mvi.Abstractions.MVI.State.IMviState
            {
                public T Items { get; init; } = default!;
                public int Version { get; set; }
            }
            public class State : Base<System.Collections.Generic.List<int>> { }
            public sealed class ChildState : State { }
            """;
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source);
        string[] locations = diagnostics.Select(diagnostic => diagnostic.Location.SourceTree!.GetText()
            .ToString(diagnostic.Location.SourceSpan)).Order().ToArray();
        await Assert.That(string.Join(",", locations)).IsEqualTo("State,Version");
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source)
    {
        Compilation compilation = GeneratorTestHost.CreateCompilation(source, GeneratorTestHost.FrameworkReferences);
        await Assert.That(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray()).IsEmpty();
        CompilationWithAnalyzers analysis = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new MiKiNuoArchitectureAnalyzer()));
        ImmutableArray<Diagnostic> diagnostics = await analysis.GetAnalyzerDiagnosticsAsync();
        return diagnostics.Where(diagnostic => diagnostic.Id == "MVI0204").ToImmutableArray();
    }
}
