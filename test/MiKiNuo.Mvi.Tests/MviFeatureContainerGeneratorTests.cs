using Microsoft.CodeAnalysis;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Domain.MVI.State;
using MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 [MviFeature] 容器装配生成器的行为测试（引用真实框架程序集）。
/// </summary>
public sealed class MviFeatureContainerGeneratorTests
{
    private const string FeatureSource = """
        namespace FeatureTest
        {
            using MiKiNuo.Mvi.Application.MVI.Effect;
            using MiKiNuo.Mvi.Application.MVI.Reducer;
            using MiKiNuo.Mvi.Application.MVI.Store;
            using MiKiNuo.Mvi.Application.MVI.Threading;
            using MiKiNuo.Mvi.Application.MVI.ViewModel;
            using MiKiNuo.Mvi.Domain.DI;
            using MiKiNuo.Mvi.Domain.MVI.Effect;
            using MiKiNuo.Mvi.Domain.MVI.Intent;
            using MiKiNuo.Mvi.Domain.MVI.Reducer;
            using MiKiNuo.Mvi.Domain.MVI.State;

            public sealed record TestState(int Count) : IMviState
            {
                public static TestState Initial { get; } = new(0);
            }

            public abstract partial record TestIntent : IMviIntent
            {
                public sealed partial record Increment : TestIntent;
            }

            public abstract partial record TestEffect : IMviEffect
            {
                public sealed partial record Ping : TestEffect;
            }

            [MviFeature]
            public sealed partial class TestReducer : MviReducerBase<TestState, TestIntent, TestEffect>
            {
                public override MviReduceResult<TestState, TestEffect> Reduce(TestState state, TestIntent intent)
                {
                    return MviReduceResult.State<TestState, TestEffect>(state);
                }
            }

            public sealed partial class TestEffectDispatcher : MviEffectDispatcherBase<TestIntent, TestEffect>
            {
                protected override System.Threading.Tasks.ValueTask DispatchCoreAsync(
                    TestEffect effect,
                    System.Threading.CancellationToken cancellationToken)
                {
                    return System.Threading.Tasks.ValueTask.CompletedTask;
                }
            }

            public sealed partial class TestViewModel : MviViewModelBase<TestState, TestIntent, TestEffect>
            {
                public TestViewModel(IMviStore<TestState, TestIntent, TestEffect> store, IMviUiDispatcher? uiDispatcher = null)
                    : base(store, uiDispatcher)
                {
                }

                protected override void ApplyStateCore(TestState state)
                {
                }
            }
        }
        """;

    /// <summary>
    /// 验证生成器为 [MviFeature] 标记的 Reducer 装配 Store/Reducer/EffectDispatcher/ViewModel。
    /// </summary>
    [Test]
    public async Task Generator_Should_AssembleFeatureIntoContainerAsync()
    {
        (GeneratorDriverRunResult runResult, bool emitSuccess) =
            GeneratorTestHost.RunGeneratorAndCompile<MviDiContainerGenerator>(
                FeatureSource,
                GetFrameworkReferences());

        string generated = string.Join("\n", runResult.GeneratedTrees.Select(tree => tree.GetText().ToString()));

        await Assert.That(generated).Contains("CreateTestStore");
        await Assert.That(generated).Contains("CreateTestEffectDispatcher");
        await Assert.That(generated).Contains("CreateTestViewModel");
        await Assert.That(generated).Contains("TestState.Initial");
        await Assert.That(emitSuccess).IsTrue();
    }

    /// <summary>
    /// 验证状态类型缺少公开静态 Initial 属性时报告 MVI0016 且跳过装配。
    /// </summary>
    [Test]
    public async Task Generator_Should_ReportMvi0016WhenInitialMissingAsync()
    {
        string brokenSource = FeatureSource.Replace(
            "public static TestState Initial { get; } = new(0);",
            string.Empty);

        GeneratorDriverRunResult runResult = GeneratorTestHost.RunGenerator<MviDiContainerGenerator>(
            brokenSource,
            GetFrameworkReferences());

        await Assert.That(runResult.Diagnostics.Any(d => d.Id == "MVI0016")).IsTrue();
        string generated = string.Join("\n", runResult.GeneratedTrees.Select(tree => tree.GetText().ToString()));
        await Assert.That(generated).DoesNotContain("CreateTestStore");
    }

    private static MetadataReference[] GetFrameworkReferences()
    {
        return
        [
            MetadataReference.CreateFromFile(typeof(IMviState).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IMviStore<,,>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(R3.Observable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.ComponentModel.INotifyPropertyChanged).Assembly.Location),
        ];
    }
}
