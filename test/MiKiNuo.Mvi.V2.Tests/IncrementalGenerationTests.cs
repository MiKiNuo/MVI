using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.V2.Tests;

/// <summary>验证局部消费者变更不会重新生成无关功能。</summary>
public sealed class IncrementalGenerationTests
{
    /// <summary>验证无关变更与局部变更复用未受影响的功能生成结果。</summary>
    /// <returns>表示增量生成步骤验证完成的任务。</returns>
    [Test]
    public async Task UnrelatedAndLocalEditsReuseUnaffectedFeatureEmission()
    {
        const string first = "using MiKiNuo.Mvi; namespace A; public sealed record State { [Input] public int Count { get; init; } } public sealed partial class Editor() : Feature<State>(new());";
        const string second = "using MiKiNuo.Mvi; namespace B; public sealed record State { [Input] public string Name { get; init; } = \"\"; } public sealed partial class Editor() : Feature<State>(new());";
        CSharpCompilation compilation = GeneratorTestHost.Compilation(first, second, "public sealed class Unrelated { public int Value => 1; }");
        GeneratorDriver driver = GeneratorTestHost.Driver().RunGenerators(compilation);
        SyntaxTree unrelated = compilation.SyntaxTrees.Last();
        compilation = compilation.ReplaceSyntaxTree(unrelated, CSharpSyntaxTree.ParseText("public sealed class Unrelated { public int Value => 2; }",
            new CSharpParseOptions(LanguageVersion.Latest), unrelated.FilePath));
        driver = driver.RunGenerators(compilation);
        IncrementalStepRunReason[] unrelatedReasons = EmissionReasons(driver);
        await Assert.That(unrelatedReasons.Length).IsEqualTo(2);
        await Assert.That(unrelatedReasons.All(static reason => reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged)).IsTrue();

        SyntaxTree originalFirst = compilation.SyntaxTrees.First();
        compilation = compilation.ReplaceSyntaxTree(originalFirst, CSharpSyntaxTree.ParseText(first.Replace("int Count", "int Total", StringComparison.Ordinal),
            new CSharpParseOptions(LanguageVersion.Latest), originalFirst.FilePath));
        driver = driver.RunGenerators(compilation);
        IncrementalStepRunReason[] localReasons = EmissionReasons(driver);
        await Assert.That(localReasons.Count(static reason => reason == IncrementalStepRunReason.Modified)).IsEqualTo(1);
        await Assert.That(localReasons.Count(static reason => reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged)).IsEqualTo(1);
        await Assert.That(driver.GetRunResult().GeneratedTrees.Single(static tree => tree.FilePath.EndsWith("A.Editor.Inputs.g.cs", StringComparison.Ordinal))
            .ToString().Contains("SetTotal", StringComparison.Ordinal)).IsTrue();
        await Assert.That(driver.GetRunResult().Diagnostics.IsEmpty).IsTrue();
    }

    private static IncrementalStepRunReason[] EmissionReasons(GeneratorDriver driver)
        => driver.GetRunResult().Results.Single().TrackedSteps["FeatureEmission"]
            .SelectMany(static step => step.Outputs).Select(static output => output.Reason).ToArray();
}
