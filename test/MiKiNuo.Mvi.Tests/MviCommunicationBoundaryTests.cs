using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using MiKiNuo.Mvi.Infrastructure.BuildTime.Analyzers;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>验证 Feature 内部禁止直接访问其他 Feature 的状态。</summary>
public sealed class MviCommunicationBoundaryTests
{
    /// <summary>旧兄弟订阅入口在业务 ViewModel 内必须被拒绝。</summary>
    [Test]
    public async Task SiblingSubscriptionIsRejectedAsync()
    {
        string source = MviFeatureContainerGeneratorTests.FeatureSource.Replace(
            "protected override void ApplyStateCore(TestState state)",
            "public void Connect(IMviStore<TestState, TestIntent, TestEffect> other) { BindSiblingState(other, _ => { }); } protected override void ApplyStateCore(TestState state)");
        Compilation compilation = GeneratorTestHost.CreateCompilation(source, MviFeatureContainerGeneratorTests.GetFrameworkReferences());
        ImmutableArray<Diagnostic> diagnostics = await compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new MviCommunicationBoundaryAnalyzer())).GetAnalyzerDiagnosticsAsync();
        await Assert.That(diagnostics.Any(diagnostic => diagnostic.Id == "MVI0021")).IsTrue();
    }
}
