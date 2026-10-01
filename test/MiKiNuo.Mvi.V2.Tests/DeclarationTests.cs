using Microsoft.CodeAnalysis;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.V2.Tests;

/// <summary>验证普通消费者声明的生成和边界诊断。</summary>
public sealed class DeclarationTests
{
    /// <summary>验证普通消费者的别名、继承输入和不可变数据能够编译。</summary>
    /// <returns>表示消费者编译验证完成的任务。</returns>
    [Test]
    public async Task OrdinaryConsumerCompilesAliasesInheritedInputsAndImmutableData()
    {
        (Compilation compilation, GeneratorDriverRunResult result) = GeneratorTestHost.Run("""
            using System.Collections.Immutable;
            using MiKiNuo.Mvi;
            using Base = MiKiNuo.Mvi.Feature<Demo.State>;
            namespace Demo {
                public record Parent { [Input] public string Name { get; init; } = ""; }
                public readonly record struct Item(int Id);
                public sealed record State : Parent {
                    public ImmutableArray<Item> Items { get; init; } = [];
                    [Input] public string? Optional { get; init; }
                    public string ReadOnly => Name;
                }
                public sealed partial class Editor() : Base(new());
                public sealed partial class StructEditor() : Feature<Item>(new());
                public static class Program {
                    public static void Run() { var editor = new Editor(); editor.SetName("value"); editor.SetOptional(null); }
                }
            }
            """);

        await Assert.That(result.Diagnostics.IsEmpty).IsTrue();
        await Assert.That(compilation.GetDiagnostics().Count(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)).IsEqualTo(0);
        await Assert.That(result.GeneratedTrees.Any(static tree => tree.ToString().Contains("SetName"))).IsTrue();
        await Assert.That(result.GeneratedTrees.Any(static tree => tree.ToString().Contains("SetReadOnly"))).IsFalse();
    }

    /// <summary>验证状态成员不能保存深层可变数据。</summary>
    /// <param name="member">需要诊断的状态成员声明。</param>
    /// <param name="nested">该成员引用的嵌套类型声明。</param>
    /// <returns>表示不可变约束验证完成的任务。</returns>
    [Test]
    [Arguments("public int[] Values { get; init; } = [];", "")]
    [Arguments("public System.Collections.Generic.IReadOnlyList<int> Values { get; init; } = new int[0];", "")]
    [Arguments("public System.Collections.ObjectModel.ReadOnlyCollection<int> Values { get; init; } = new(new int[0]);", "")]
    [Arguments("public Nested Value { get; init; } = new();", "public sealed class Nested { public int Value { get; set; } }")]
    [Arguments("public Nested Value { get; init; } = new();", "public sealed record Nested { public int Value { get; set; } }")]
    [Arguments("public System.Collections.Immutable.ImmutableArray<System.Collections.Generic.List<int>> Values { get; init; } = [];", "")]
    [Arguments("public int Value;", "")]
    [Arguments("public int Value { get; set; }", "")]
    public async Task DeeplyMutableMembersAreRejected(string member, string nested)
    {
        (Compilation _, GeneratorDriverRunResult result) = GeneratorTestHost.Run("using MiKiNuo.Mvi; " + nested
            + " public sealed record State { [Input] public string Name { get; init; } = \"\"; " + member
            + " } public sealed partial class Editor() : Feature<State>(new());");
        await Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Id == "MVI2002" && diagnostic.Location.IsInSource)).IsTrue();
        await Assert.That(result.GeneratedTrees.Length).IsEqualTo(0);
    }

    /// <summary>验证密封状态记录不能继承可写的业务状态。</summary>
    /// <returns>表示继承成员诊断验证完成的任务。</returns>
    [Test]
    public async Task SealedRecordCannotHideInheritedMutableState()
    {
        (Compilation _, GeneratorDriverRunResult result) = GeneratorTestHost.Run("""
            using MiKiNuo.Mvi;
            public record Parent { public int Value { get; set; } }
            public sealed record State : Parent { [Input] public string Name { get; init; } = ""; }
            public sealed partial class Editor() : Feature<State>(new());
            """);
        await Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Id == "MVI2002"
            && diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan) == "Value")).IsTrue();
    }

    /// <summary>验证不符合精确签名的纯转换方法收到原声明诊断。</summary>
    /// <param name="method">需要验证的纯转换方法声明。</param>
    /// <param name="extra">该方法引用的附加类型声明。</param>
    /// <returns>表示规则签名验证完成的任务。</returns>
    [Test]
    [Arguments("public static State Change(State state, int value) => state;", "")]
    [Arguments("private State Change(State state, int value) => state;", "")]
    [Arguments("private static State Change<T>(State state, int value) => state;", "")]
    [Arguments("private static State Change(State state, long value) => state;", "")]
    [Arguments("private static State Change(State state, ref int value) => state;", "")]
    [Arguments("private static State Change(State state, int value = 0) => state;", "")]
    [Arguments("private static Fake.State Change(State state, int value) => new();", "namespace Fake { public sealed record State; }")]
    [Arguments("private static State Change(Fake.State state, int value) => new();", "namespace Fake { public sealed record State; }")]
    public async Task IncorrectRuleShapesReceiveTargetedDiagnostic(string method, string extra)
    {
        (Compilation _, GeneratorDriverRunResult result) = GeneratorTestHost.Run("using MiKiNuo.Mvi; " + extra
            + " public sealed record State { [Input] public int Count { get; init; } }"
            + " public sealed partial class Editor() : Feature<State>(new()) { [OnInput(nameof(State.Count))] " + method + " }");
        await Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Id == "MVI2004" && diagnostic.Location.IsInSource)).IsTrue();
        await Assert.That(result.GeneratedTrees.Length).IsEqualTo(0);
    }

    /// <summary>验证输入错误、规则歧义和入口冲突定位到原始声明。</summary>
    /// <param name="property">带输入标记的状态成员声明。</param>
    /// <param name="id">预期产生的诊断编号。</param>
    /// <param name="members">功能中的附加成员声明。</param>
    /// <returns>表示输入和成员边界验证完成的任务。</returns>
    [Test]
    [Arguments("[Input] public int Count { get; }", "MVI2003", "")]
    [Arguments("[Input] public static int Count { get; init; }", "MVI2003", "")]
    [Arguments("[Input] public int Count;", "MVI2003", "")]
    [Arguments("[Input] internal int Count { get; init; }", "MVI2003", "")]
    [Arguments("[Input] public int Count { get; private init; }", "MVI2003", "")]
    [Arguments("[Input] public int Count { get; init; }", "MVI2006", "public void SetCount(int value) { }")]
    [Arguments("[Input] public int Count { get; init; }", "MVI2005", "[OnInput(\"Count\")] private static State First(State s, int v) => s; [OnInput(\"Count\")] private static State Second(State s, int v) => s;")]
    [Arguments("[Input] public int Count { get; init; }", "MVI2004", "[OnInput(\"Unknown\")] private static State Change(State s, int v) => s;")]
    public async Task InvalidInputAmbiguityAndConflictsAreLocated(string property, string id, string members)
    {
        (Compilation _, GeneratorDriverRunResult result) = GeneratorTestHost.Run("using MiKiNuo.Mvi; public sealed record State { "
            + property + " } public sealed partial class Editor() : Feature<State>(new()) { " + members + " }");
        await Assert.That(result.Diagnostics.Any(diagnostic => diagnostic.Id == id && diagnostic.Location.IsInSource)).IsTrue();
        await Assert.That(result.GeneratedTrees.Length).IsEqualTo(0);
    }

    /// <summary>验证非 partial 功能会被拒绝且假同名属性不参与生成。</summary>
    /// <returns>表示功能声明和属性身份验证完成的任务。</returns>
    [Test]
    public async Task NonPartialFeatureIsDiagnosedAndSameNamedAttributesAreIgnored()
    {
        (Compilation _, GeneratorDriverRunResult invalid) = GeneratorTestHost.Run("using MiKiNuo.Mvi; public sealed record State; public sealed class Editor() : Feature<State>(new());");
        await Assert.That(invalid.Diagnostics.Any(static diagnostic => diagnostic.Id == "MVI2001")).IsTrue();

        (Compilation compilation, GeneratorDriverRunResult valid) = GeneratorTestHost.Run("""
            using MiKiNuo.Mvi;
            public sealed record State { [Input] public int Count { get; init; } [Fake.Input] public int Ignored { get; init; } }
            public sealed partial class Editor() : Feature<State>(new()) {
                [Fake.OnInput("Count")] private static void FakeRule() { }
            }
            namespace Fake {
                public sealed class InputAttribute : System.Attribute { }
                public sealed class OnInputAttribute(string name) : System.Attribute { }
            }
            """);
        await Assert.That(valid.Diagnostics.IsEmpty).IsTrue();
        await Assert.That(compilation.GetDiagnostics().Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)).IsFalse();
        await Assert.That(valid.GeneratedTrees.Single().ToString().Contains("SetIgnored")).IsFalse();
    }

    /// <summary>验证按引用传递状态的输入规则收到框架诊断。</summary>
    /// <returns>表示非法规则签名验证完成的任务。</returns>
    [Test]
    public async Task InvalidRuleSignatureReceivesFrameworkDiagnostic()
    {
        (Compilation _, GeneratorDriverRunResult result) = GeneratorTestHost.Run("""
            using MiKiNuo.Mvi;
            public sealed record State { [Input] public int Count { get; init; } }
            public sealed partial class Editor() : Feature<State>(new())
            {
                [OnInput(nameof(State.Count))]
                private static State Change(in State state, int value) => state with { Count = value };
            }
            """);

        await Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Id == "MVI2004")).IsTrue();
    }

    /// <summary>验证嵌套可变集合在原始状态成员处被拒绝。</summary>
    /// <returns>表示嵌套状态诊断验证完成的任务。</returns>
    [Test]
    public async Task MutableNestedStateIsRejectedAtItsDeclaration()
    {
        (Compilation _, GeneratorDriverRunResult result) = GeneratorTestHost.Run("""
            using System.Collections.Generic;
            using MiKiNuo.Mvi;
            public sealed record State { [Input] public string Name { get; init; } = ""; public List<int> Values { get; init; } = []; }
            public sealed partial class Editor() : Feature<State>(new());
            """);

        await Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Id == "MVI2002"
            && diagnostic.Location.IsInSource)).IsTrue();
    }
}
