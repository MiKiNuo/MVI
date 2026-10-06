using MiKiNuo.Mvi.Abstractions.MVI.Mutation;
using MiKiNuo.Mvi.Abstractions.MVI.State;
using MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.LoginReplica;
using MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.Minimal;
using MiKiNuo.Mvi.Runtime.MVI.Intent;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>验证基准场景的公开入口在访问参数和修改计数之前拒绝空参数。</summary>
public sealed class BenchmarkArgumentValidationTests
{
    /// <summary>最小 Reducer 显式拒绝缺失的状态或变化，不产生空引用异常。</summary>
    [Test]
    public async Task MinimalReducerRejectsNullArgumentsAsync()
    {
        MinimalReducer reducer = new();
        await ExpectNullArgumentAsync(() =>
        {
            reducer.Reduce(null!, new MinimalMutation());
            return ValueTask.CompletedTask;
        }, "state");
        await ExpectNullArgumentAsync(() =>
        {
            reducer.Reduce(MinimalState.Initial, null!);
            return ValueTask.CompletedTask;
        }, "mutation");
    }

    /// <summary>最小 Handler 在缺少意图或上下文时不提交状态，也不增加外部调用计数。</summary>
    [Test]
    public async Task MinimalHandlerRejectsNullArgumentsAsync()
    {
        MinimalHandler handler = new(4);
        await ExpectNullArgumentAsync(() => handler.HandleAsync(null!, new UnusedContext<MinimalState>(), default), "intent");
        await ExpectNullArgumentAsync(() => handler.HandleAsync(new MinimalIntent.EmitNops(), null!, default), "context");
        await Assert.That(handler.HandledCount).IsEqualTo(0);
    }

    /// <summary>登录基准 Handler 在缺少输入或上下文时不会进入状态提交。</summary>
    [Test]
    public async Task LoginHandlerRejectsNullArgumentsAsync()
    {
        BenchLoginHandler handler = new();
        await ExpectNullArgumentAsync(() => handler.HandleAsync(null!, new UnusedContext<BenchLoginState>(), default), "intent");
        await ExpectNullArgumentAsync(() => handler.HandleAsync(new("user", "benchmark"), null!, default), "context");
    }

    /// <summary>中间件拒绝非法输入后不增加调用计数，不执行下一步。</summary>
    [Test]
    public async Task MiddlewareRejectsNullArgumentsBeforeCountingAsync()
    {
        NopMiddleware middleware = new();
        int calls = 0;
        ValueTask NextStep() { calls++; return ValueTask.CompletedTask; }
        await ExpectNullArgumentAsync(() => middleware.InvokeAsync(null!, new UnusedContext<MinimalState>(), NextStep, default), "intent");
        await ExpectNullArgumentAsync(() => middleware.InvokeAsync(new MinimalIntent.Increment(), null!, NextStep, default), "context");
        await ExpectNullArgumentAsync(() => middleware.InvokeAsync(new MinimalIntent.Increment(), new UnusedContext<MinimalState>(), null!, default), "nextStep");
        await Assert.That(middleware.InvocationCount).IsEqualTo(0);
        await Assert.That(calls).IsEqualTo(0);
    }

    /// <summary>合法中间件调用只计数一次并转发一次，不读取上下文。</summary>
    [Test]
    public async Task MiddlewareForwardsValidCallOnceAsync()
    {
        NopMiddleware middleware = new();
        int calls = 0;
        await middleware.InvokeAsync(new MinimalIntent.Increment(), new UnusedContext<MinimalState>(),
            () => { calls++; return ValueTask.CompletedTask; }, default);
        await Assert.That(middleware.InvocationCount).IsEqualTo(1);
        await Assert.That(calls).IsEqualTo(1);
    }

    private static async Task ExpectNullArgumentAsync(Func<ValueTask> action, string parameterName)
    {
        try { await action(); }
        catch (ArgumentNullException exception)
        {
            await Assert.That(exception.ParamName).IsEqualTo(parameterName);
            return;
        }
        throw new InvalidOperationException("预期拒绝空参数：" + parameterName);
    }

    private sealed class UnusedContext<TState> : IIntentContext<TState> where TState : IMviState
    {
        /// <inheritdoc/>
        public TState State => throw new InvalidOperationException("参数检查不应读取状态。");
        /// <inheritdoc/>
        public void Reduce(IMviMutation<TState> mutation) => throw new InvalidOperationException("参数检查不应提交变化。");
        /// <inheritdoc/>
        public bool TryReduce(Func<TState, bool> guard, IMviMutation<TState> mutation)
            => throw new InvalidOperationException("参数检查不应提交变化。");
    }
}
