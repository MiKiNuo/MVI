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
/// <summary>中间件围绕真实 Handler 执行且禁止重复 nextStep。</summary>
public sealed class MiddlewarePipelineTests
{
    /// <summary>验证进入与退出的顺序。</summary>
    [Test] public async Task PipelineWrapsHandlerInOrderAsync()
    {
        List<string> calls = [];
        await using var store = TestCheck.Store(new((context, _) =>
        { calls.Add("handler"); context.Reduce(new CounterMutation.Increment()); return ValueTask.CompletedTask; }),
        [new Probe("A", calls), new Probe("B", calls)]);
        await store.DispatchAsync(new CounterIntent.Run());
        await Assert.That(string.Join(",", calls)).IsEqualTo("A+,B+,handler,B-,A-");
    }
    /// <summary>重入 nextStep 不会重复执行业务。</summary>
    [Test] public async Task CallingNextTwiceFailsWithoutSecondCommitAsync()
    {
        await using var store = TestCheck.Store(middlewares: [new Twice()]);
        await TestCheck.ThrowsAsync<InvalidOperationException>(() => store.DispatchAsync(new CounterIntent.Increment()).AsTask());
        await Assert.That(store.CurrentState.Count).IsEqualTo(1);
    }
    /// <summary>可完全拒绝输入，不触发状态转换。</summary>
    [Test] public async Task MiddlewareCanRejectBeforeHandlerAsync()
    {
        await using var store = TestCheck.Store(middlewares: [new Reject()]);
        await store.DispatchAsync(new CounterIntent.Increment());
        await Assert.That(store.CurrentState.Count).IsEqualTo(0);
    }
    /// <summary>公开中间件契约支持 nextStep 命名实参，并保持一次顺序调用。</summary>
    [Test] public async Task MiddlewareAcceptsNextStepNamedArgumentAsync()
    {
        List<string> calls = [];
        IMviMiddleware<CounterState, CounterIntent> middleware = new Probe("A", calls);
        await using var store = TestCheck.Store(new((context, token) => middleware.InvokeAsync(
            intent: new CounterIntent.Run(),
            context: context,
            nextStep: () => { calls.Add("terminal"); return ValueTask.CompletedTask; },
            cancellationToken: token)));
        await store.DispatchAsync(new CounterIntent.Run());
        await Assert.That(string.Join(",", calls)).IsEqualTo("A+,terminal,A-");
    }
    private sealed class Probe : IMviMiddleware<CounterState, CounterIntent>
    {
        private readonly string _name;
        private readonly List<string> _calls;

        /// <summary>初始化用于记录中间件调用顺序的探针。</summary>
        /// <param name="name">当前中间件的记录名称。</param>
        /// <param name="calls">用于保存调用顺序的共享记录集合。</param>
        public Probe(string name, List<string> calls)
        {
            _name = name;
            _calls = calls;
        }

        /// <summary>记录管线顺序。</summary>
        public async ValueTask InvokeAsync(
            CounterIntent intent,
            IIntentContext<CounterState> context,
            MviNext nextStep,
            CancellationToken cancellationToken)
        {
            _calls.Add(_name + "+");
            await nextStep();
            _calls.Add(_name + "-");
        }
    }
    private sealed class Twice : IMviMiddleware<CounterState, CounterIntent>
    {
        /// <summary>模拟错误的重复调用。</summary>
        public async ValueTask InvokeAsync(CounterIntent intent, IIntentContext<CounterState> context, MviNext nextStep, CancellationToken cancellationToken) { await nextStep(); await nextStep(); }
    }
    private sealed class Reject : IMviMiddleware<CounterState, CounterIntent>
    {
        /// <summary>明确拒绝。</summary>
        public ValueTask InvokeAsync(CounterIntent intent, IIntentContext<CounterState> context, MviNext nextStep, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }
}
