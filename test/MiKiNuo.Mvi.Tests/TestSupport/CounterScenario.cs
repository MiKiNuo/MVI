using MiKiNuo.Mvi.Abstractions.MVI.Intent;
using MiKiNuo.Mvi.Abstractions.MVI.Mutation;
using MiKiNuo.Mvi.Abstractions.MVI.State;
using MiKiNuo.Mvi.Runtime.MVI.Intent;
using MiKiNuo.Mvi.Runtime.MVI.Reducer;
using MiKiNuo.Mvi.Runtime.MVI.Store;
using MiKiNuo.Mvi.Runtime.MVI.Middleware;
using R3;
using TUnit.Assertions;
using TUnit.Core;
namespace MiKiNuo.Mvi.Tests;
/// <summary>测试用不可变状态；也驱动真实 StatePath 生成。</summary>
/// <param name="Count">当前累计计数。</param>
/// <param name="Label">当前测试名称。</param>
/// <param name="IsBusy">当前是否处于受控异步操作中。</param>
public sealed record CounterState(int Count = 0, string Label = "", bool IsBusy = false) : IMviState
{
    /// <summary>获取初始状态。</summary>
    public static CounterState Initial { get; } = new();
}
/// <summary>输入契约只负责表达操作。</summary>
public abstract record CounterIntent : IMviIntent
{
    /// <summary>增加计数。</summary>
    public sealed record Increment : CounterIntent;
    /// <summary>更改名称。</summary>
    /// <param name="Label">新的测试名称。</param>
    public sealed record Rename(string Label) : CounterIntent;
    /// <summary>测试等待与失败路径的操作。</summary>
    public sealed record Run : CounterIntent;
}
/// <summary>规约输入与用户意图不同类型。</summary>
public abstract record CounterMutation : IMviMutation<CounterState>
{
    /// <summary>增量计数。</summary>
    public sealed record Increment : CounterMutation;
    /// <summary>替换名称。</summary>
    /// <param name="Label">需要提交的新名称。</param>
    public sealed record Rename(string Label) : CounterMutation;
    /// <summary>切换忙碌状态。</summary>
    /// <param name="Value">新的忙碌状态。</param>
    public sealed record Busy(bool Value) : CounterMutation;
}
/// <summary>真实 Store 使用的纯 Reducer。</summary>
public sealed class CounterReducer : IMviReducer<CounterState>
{
    /// <summary>应用单个内部变化。</summary>
    public CounterState Reduce(CounterState state, IMviMutation<CounterState> mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);

        return mutation switch
        {
            CounterMutation.Increment => state with { Count = state.Count + 1 },
            CounterMutation.Rename value => state with { Label = value.Label },
            CounterMutation.Busy value => state with { IsBusy = value.Value },
            _ => throw new ArgumentException("未知变化。", nameof(mutation)),
        };
    }
}
/// <summary>可注入受控异步操作的处理器。</summary>
/// <param name="operation">可选的受控异步操作，用于测试等待、取消与并发路径。</param>
public sealed class CounterHandler(Func<IIntentContext<CounterState>, CancellationToken, ValueTask>? operation = null) : IIntentHandler<CounterIntent, CounterState>
{
    /// <summary>经过实际上下文应用 Mutation。</summary>
    public ValueTask HandleAsync(CounterIntent intent, IIntentContext<CounterState> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(context);

        switch (intent)
        {
            case CounterIntent.Increment: context.Reduce(new CounterMutation.Increment()); break;
            case CounterIntent.Rename rename: context.Reduce(new CounterMutation.Rename(rename.Label)); break;
            case CounterIntent.Run when operation is not null: return operation(context, cancellationToken);
        }
        return ValueTask.CompletedTask;
    }
}
/// <summary>通过明确的同步信号验证行为，不依赖任意睡眠。</summary>
internal static class TestCheck
{
    /// <summary>创建异步完成信号。</summary>
    internal static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    /// <summary>验证且返回预期异常；反例必须使测试失败。</summary>
    internal static async Task<T> ThrowsAsync<T>(Func<Task> action) where T : Exception
    {
        try { await action(); }
        catch (T expected) { return expected; }
        throw new InvalidOperationException("应抛出 " + typeof(T).Name);
    }
    /// <summary>创建有默认测试依赖的唯一内核。</summary>
    internal static MviStore<CounterState, CounterIntent> Store(CounterHandler? handler = null,
        IReadOnlyList<IMviMiddleware<CounterState, CounterIntent>>? middlewares = null, int capacity = 64)
        => new(CounterState.Initial, handler ?? new(), new CounterReducer(), middlewares, maxConcurrentIntents: capacity);
}
