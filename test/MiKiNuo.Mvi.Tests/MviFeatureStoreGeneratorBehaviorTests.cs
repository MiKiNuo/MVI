using Microsoft.CodeAnalysis;
using MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;
using MiKiNuo.Mvi.Tests.TestSupport;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 MviDiContainerGenerator 的 Feature Store 装配行为测试。
/// 验证 [MviFeatureModule] 驱动的 Store 工厂生成与诊断。
/// </summary>
public sealed class MviFeatureStoreGeneratorBehaviorTests
{
    /// <summary>
    /// 验证完整 Feature 产出可编译的 Store 工厂与 Resolve 分支。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceFeatureStoreFactoryAsync()
    {
        (GeneratorDriverRunResult runResult, bool emitSuccess) =
            GeneratorTestHost.RunGeneratorAndCompile<MviDiContainerGenerator>(
                FeatureSource + "\n" + StubDefinitions);

        await Assert.That(emitSuccess).IsTrue();

        string generatedCode = runResult.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("CreateCounterFeatureStore");
        await Assert.That(generatedCode).Contains("FeatureCounterState.Initial");
        await Assert.That(generatedCode).Contains("IMviStore<");
        await Assert.That(generatedCode).Contains("FeatureCounterIntentHandler()");
        await Assert.That(generatedCode).Contains("FeatureUnitEffectDispatcher()");
    }

    /// <summary>
    /// 验证状态缺少 static Initial 时报告 MVI0015 诊断。
    /// </summary>
    [Test]
    public async Task Generate_Should_ReportMVI0015_WhenStateMissingInitialAsync()
    {
        GeneratorDriverRunResult runResult =
            GeneratorTestHost.RunGenerator<MviDiContainerGenerator>(
                FeatureSourceWithoutInitial + "\n" + StubDefinitions);

        await Assert.That(runResult.Diagnostics.Any(d => d.Id == "MVI0015")).IsTrue();
    }

    /// <summary>
    /// 验证缺少唯一 IntentHandler 实现时报告 MVI0016 诊断。
    /// </summary>
    [Test]
    public async Task Generate_Should_ReportMVI0016_WhenHandlerMissingAsync()
    {
        GeneratorDriverRunResult runResult =
            GeneratorTestHost.RunGenerator<MviDiContainerGenerator>(
                FeatureSourceWithoutHandler + "\n" + StubDefinitions);

        await Assert.That(runResult.Diagnostics.Any(d => d.Id == "MVI0016")).IsTrue();
    }

    /// <summary>
    /// 桩类型定义：DI 容器运行时 + MVI Store 运行时。
    /// </summary>
    private const string StubDefinitions =
        GeneratorTestStubs.DiContainerRuntime + "\n" + GeneratorTestStubs.MviStoreRuntime;

    /// <summary>
    /// 完整 Feature 源码。
    /// </summary>
    private const string FeatureSource = """
        namespace MiKiNuo.Mvi.Tests.Samples
        {
            public sealed record FeatureCounterState(int Count) : MiKiNuo.Mvi.Domain.MVI.State.IMviState
            {
                public static FeatureCounterState Initial { get; } = new(0);
            }

            public abstract record FeatureCounterIntent : MiKiNuo.Mvi.Domain.MVI.Intent.IMviIntent
            {
                public sealed record Increment : FeatureCounterIntent;
            }

            public sealed record FeatureUnitEffect : MiKiNuo.Mvi.Domain.MVI.Effect.IMviEffect;

            [MiKiNuo.Mvi.Domain.MVI.Feature.MviFeatureModule("Counter")]
            public sealed class FeatureCounterReducer
                : MiKiNuo.Mvi.Application.MVI.Reducer.IMviReducer<FeatureCounterState, FeatureCounterIntent, FeatureUnitEffect>
            {
                public MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceResult<FeatureCounterState, FeatureUnitEffect> Reduce(
                    FeatureCounterState state,
                    FeatureCounterIntent intent,
                    MiKiNuo.Mvi.Domain.MVI.Business.IMviBusinessResult? result = null)
                    => new(state);
            }

            public sealed class FeatureCounterIntentHandler
                : MiKiNuo.Mvi.Application.MVI.IntentHandler.IMviIntentHandler<FeatureCounterState, FeatureCounterIntent, FeatureUnitEffect>
            {
                public async System.Threading.Tasks.ValueTask<MiKiNuo.Mvi.Domain.MVI.Business.IMviBusinessResult?> HandleAsync(
                    FeatureCounterState state,
                    FeatureCounterIntent intent,
                    System.Threading.CancellationToken cancellationToken = default)
                {
                    await System.Threading.Tasks.Task.CompletedTask;
                    return null;
                }
            }

            public sealed class FeatureUnitEffectDispatcher
                : MiKiNuo.Mvi.Application.MVI.Effect.IMviEffectDispatcher<FeatureUnitEffect>
            {
                public System.Threading.Tasks.ValueTask DispatchAsync(
                    FeatureUnitEffect effect,
                    System.Threading.CancellationToken cancellationToken = default)
                    => System.Threading.Tasks.ValueTask.CompletedTask;
            }
        }
        """;

    /// <summary>
    /// 缺少 static Initial 的 Feature 源码。
    /// </summary>
    private const string FeatureSourceWithoutInitial = """
        namespace MiKiNuo.Mvi.Tests.Samples
        {
            public sealed record NoInitialState(int Count) : MiKiNuo.Mvi.Domain.MVI.State.IMviState;

            public abstract record NoInitialIntent : MiKiNuo.Mvi.Domain.MVI.Intent.IMviIntent;

            public sealed record NoInitialEffect : MiKiNuo.Mvi.Domain.MVI.Effect.IMviEffect;

            [MiKiNuo.Mvi.Domain.MVI.Feature.MviFeatureModule("NoInitial")]
            public sealed class NoInitialReducer
                : MiKiNuo.Mvi.Application.MVI.Reducer.IMviReducer<NoInitialState, NoInitialIntent, NoInitialEffect>
            {
                public MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceResult<NoInitialState, NoInitialEffect> Reduce(
                    NoInitialState state,
                    NoInitialIntent intent,
                    MiKiNuo.Mvi.Domain.MVI.Business.IMviBusinessResult? result = null)
                    => new(state);
            }
        }
        """;

    /// <summary>
    /// 缺少 IntentHandler 实现的 Feature 源码。
    /// </summary>
    private const string FeatureSourceWithoutHandler = """
        namespace MiKiNuo.Mvi.Tests.Samples
        {
            public sealed record NoHandlerState(int Count) : MiKiNuo.Mvi.Domain.MVI.State.IMviState
            {
                public static NoHandlerState Initial { get; } = new(0);
            }

            public abstract record NoHandlerIntent : MiKiNuo.Mvi.Domain.MVI.Intent.IMviIntent;

            public sealed record NoHandlerEffect : MiKiNuo.Mvi.Domain.MVI.Effect.IMviEffect;

            [MiKiNuo.Mvi.Domain.MVI.Feature.MviFeatureModule("NoHandler")]
            public sealed class NoHandlerReducer
                : MiKiNuo.Mvi.Application.MVI.Reducer.IMviReducer<NoHandlerState, NoHandlerIntent, NoHandlerEffect>
            {
                public MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceResult<NoHandlerState, NoHandlerEffect> Reduce(
                    NoHandlerState state,
                    NoHandlerIntent intent,
                    MiKiNuo.Mvi.Domain.MVI.Business.IMviBusinessResult? result = null)
                    => new(state);
            }
        }
        """;
}
