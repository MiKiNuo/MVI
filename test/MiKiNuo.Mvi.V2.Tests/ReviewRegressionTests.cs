using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.V2.Tests;

/// <summary>验证审查发现的生成入口和跨程序集状态边界。</summary>
public sealed class ReviewRegressionTests
{
    /// <summary>验证多个 partial 声明重复基类时只生成一次公开入口。</summary>
    /// <returns>表示消费者编译验证完成的任务。</returns>
    [Test]
    public async Task RepeatedPartialBaseDeclarationsGenerateOneInputEntry()
    {
        (Compilation compilation, GeneratorDriverRunResult result) = GeneratorTestHost.Run("""
            using MiKiNuo.Mvi;
            public sealed record State { [Input] public int Count { get; init; } }
            public sealed partial class Editor() : Feature<State>(new());
            """, """
            using MiKiNuo.Mvi;
            public sealed partial class Editor : Feature<State>;
            public static class Consumer { public static void Run() => new Editor().SetCount(1); }
            """);

        await Assert.That(result.Diagnostics.IsEmpty).IsTrue();
        await Assert.That(result.GeneratedTrees.Length).IsEqualTo(1);
        await Assert.That(compilation.GetDiagnostics().Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)).IsFalse();
    }

    /// <summary>验证入口名称与功能类型名称冲突时定位原类型声明。</summary>
    /// <returns>表示诊断验证完成的任务。</returns>
    [Test]
    public async Task InputEntryCannotHaveItsFeatureTypeName()
    {
        (Compilation _, GeneratorDriverRunResult result) = GeneratorTestHost.Run("""
            using MiKiNuo.Mvi;
            public sealed record State { [Input] public int Count { get; init; } }
            public sealed partial class SetCount() : Feature<State>(new());
            """);

        await Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Id == "MVI2006"
            && diagnostic.Location.IsInSource
            && diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan) == "SetCount")).IsTrue();
        await Assert.That(result.GeneratedTrees.Length).IsEqualTo(0);
    }

    /// <summary>验证外部程序集隐藏的可变记录存储不能进入状态快照。</summary>
    /// <param name="consumer">直接、嵌套或继承外部记录的消费者声明。</param>
    /// <returns>表示外部程序集和消费者边界验证完成的任务。</returns>
    [Test]
    [Arguments("public sealed partial class Editor() : Feature<External.State>(new());")]
    [Arguments("public sealed record State { public External.State Value { get; init; } = new(); } public sealed partial class Editor() : Feature<State>(new());")]
    [Arguments("public sealed record State : External.Parent { [Input] public string Name { get; init; } = \"\"; } public sealed partial class Editor() : Feature<State>(new());")]
    public async Task ImportedMutableRecordStorageIsRejected(string consumer)
    {
        CSharpCompilation external = GeneratorTestHost.Compilation("""
            using System.Collections.Generic;
            namespace External;
            public sealed record State
            {
                private readonly List<int> values = new();
                public int Count => values.Count;
                public void Add(int value) => values.Add(value);
            }
            public record Parent
            {
                private readonly List<int> values = new();
                public int Count => values.Count;
                public void Add(int value) => values.Add(value);
            }
            """).WithAssemblyName("ExternalState");
        using MemoryStream image = new();
        EmitResult emitted = external.Emit(image);
        await Assert.That(emitted.Success).IsTrue();
        MetadataReference reference = MetadataReference.CreateFromImage(image.ToArray());
        CSharpCompilation input = GeneratorTestHost.Compilation("using MiKiNuo.Mvi; " + consumer).AddReferences(reference);
        GeneratorDriver driver = GeneratorTestHost.Driver().RunGeneratorsAndUpdateCompilation(input, out Compilation output, out _);
        GeneratorDriverRunResult result = driver.GetRunResult();

        await Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Id == "MVI2002" && diagnostic.Location.IsInSource)).IsTrue();
        await Assert.That(result.GeneratedTrees.Length).IsEqualTo(0);
        await Assert.That(output.GetDiagnostics().Any(static diagnostic => diagnostic.Id == "CS8785")).IsFalse();
    }
}
