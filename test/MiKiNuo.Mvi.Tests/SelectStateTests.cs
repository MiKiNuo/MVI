using MiKiNuo.Mvi.Application.MVI.IntentHandler;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Domain.MVI.Business;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Feature;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.State;
using MiKiNuo.Mvi.Tests.TestSupport;
using R3;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 <see cref="MviStoreStateExtensions"/> 的 SelectState 行为测试。
/// 使用源生成器产出的 CounterStatePaths 进行端到端验证。
/// </summary>
public sealed class SelectStateTests
{
    /// <summary>
    /// 验证 SelectState 输出初始值与状态变化后的新值。
    /// </summary>
    [Test]
    public async Task SelectState_Should_EmitInitialAndChangedValuesAsync()
    {
        using MviStore<CounterState, CounterIntent, UnitEffect> store = CreateStore();
        List<int> values = new();
        using IDisposable subscription = store
            .SelectState(CounterStatePaths.Count)
            .Subscribe(values, static (value, list) => list.Add(value));

        await store.DispatchAsync(new CounterIntent.Increment());
        await store.DispatchAsync(new CounterIntent.Increment());

        await Assert.That(values).IsEquivalentTo(new[] { 0, 1, 2 });
        await Assert.That(CounterStatePaths.Count.DisplayPath).IsEqualTo("Count");
    }

    /// <summary>
    /// 验证 SelectState 对未变化的路径值去重。
    /// </summary>
    [Test]
    public async Task SelectState_Should_DistinctUnchangedValuesAsync()
    {
        using MviStore<CounterState, CounterIntent, UnitEffect> store = CreateStore();
        List<int> values = new();
        using IDisposable subscription = store
            .SelectState(CounterStatePaths.Count)
            .Subscribe(values, static (value, list) => list.Add(value));

        await store.DispatchAsync(new CounterIntent.Rename("a"));
        await store.DispatchAsync(new CounterIntent.Rename("b"));

        await Assert.That(values).IsEquivalentTo(new[] { 0 });
    }

    /// <summary>
    /// 验证双路径 SelectState 在任一路径变化时投影最新值。
    /// </summary>
    [Test]
    public async Task SelectState_Should_ProjectTwoPathsAsync()
    {
        using MviStore<CounterState, CounterIntent, UnitEffect> store = CreateStore();
        List<string> values = new();
        using IDisposable subscription = store
            .SelectState(
                CounterStatePaths.Count,
                CounterStatePaths.Label,
                static (count, label) => count + ":" + label)
            .Subscribe(values, static (value, list) => list.Add(value));

        await store.DispatchAsync(new CounterIntent.Increment());
        await store.DispatchAsync(new CounterIntent.Rename("x"));

        await Assert.That(values).IsEquivalentTo(new[] { "0:", "1:", "1:x" });
    }

    private static MviStore<CounterState, CounterIntent, UnitEffect> CreateStore()
    {
        return new MviStore<CounterState, CounterIntent, UnitEffect>(
            new CounterState(0, string.Empty),
            new NullCounterIntentHandler(),
            new CounterReducer(),
            new NoopEffectDispatcher<UnitEffect>());
    }
}

/// <summary>
/// 表示 SelectState 测试用计数状态。
/// </summary>
/// <param name="Count">计数值。</param>
/// <param name="Label">标签。</param>
public sealed record CounterState(int Count, string Label) : IMviState
{
    /// <summary>
    /// 获取初始状态（Feature Store 装配约定）。
    /// </summary>
    public static CounterState Initial { get; } = new(0, string.Empty);
}

/// <summary>
/// 表示 SelectState 测试用计数意图。
/// </summary>
public abstract record CounterIntent : IMviIntent
{
    /// <summary>
    /// 表示计数加一意图。
    /// </summary>
    public sealed record Increment : CounterIntent;

    /// <summary>
    /// 表示重命名标签意图。
    /// </summary>
    /// <param name="Label">新标签。</param>
    public sealed record Rename(string Label) : CounterIntent;
}

/// <summary>
/// 表示 SelectState 测试用规约器。
/// </summary>
[MviFeatureModule("Counter")]
public sealed class CounterReducer : IMviReducer<CounterState, CounterIntent, UnitEffect>
{
    /// <summary>
    /// 将意图规约为新状态。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="result">业务结果。</param>
    /// <returns>规约结果。</returns>
    public MviReduceResult<CounterState, UnitEffect> Reduce(
        CounterState state,
        CounterIntent intent,
        IMviBusinessResult? result = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return intent switch
        {
            CounterIntent.Increment => MviReduceResult.State<CounterState, UnitEffect>(
                state with { Count = state.Count + 1 }),
            CounterIntent.Rename rename => MviReduceResult.State<CounterState, UnitEffect>(
                state with { Label = rename.Label }),
            _ => MviReduceResult.State<CounterState, UnitEffect>(state),
        };
    }
}

/// <summary>
/// 表示 SelectState 测试用空意图处理器。
/// </summary>
public sealed class NullCounterIntentHandler : IMviIntentHandler<CounterState, CounterIntent, UnitEffect>
{
    /// <summary>
    /// 处理意图(空操作,返回无业务结果)。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>无业务结果。</returns>
    public async ValueTask<IMviBusinessResult?> HandleAsync(
        CounterState state,
        CounterIntent intent,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        return null;
    }
}
