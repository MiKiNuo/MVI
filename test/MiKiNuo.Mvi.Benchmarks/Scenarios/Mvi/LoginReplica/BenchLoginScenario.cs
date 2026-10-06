using MiKiNuo.Mvi.Abstractions.MVI.Binding;
using MiKiNuo.Mvi.Abstractions.MVI.Intent;
using MiKiNuo.Mvi.Abstractions.MVI.Mutation;
using MiKiNuo.Mvi.Abstractions.MVI.Reducer;
using MiKiNuo.Mvi.Abstractions.MVI.State;
using MiKiNuo.Mvi.Binding.Command;
using MiKiNuo.Mvi.Binding.ViewModel;
using MiKiNuo.Mvi.Runtime.MVI.Intent;
using MiKiNuo.Mvi.Runtime.MVI.Reducer;
using MiKiNuo.Mvi.Runtime.MVI.Store;
namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.LoginReplica;
/// <summary>登录基准只保存可公开的状态。</summary>
/// <param name="Busy">是否正在处理本次登录。</param>
/// <param name="Completed">已完成的登录次数。</param>
public sealed record BenchLoginState(bool Busy = false, int Completed = 0) : IMviState
{
    /// <summary>获取初始状态。</summary>
    public static BenchLoginState Initial { get; } = new();
}
/// <summary>只在一次派发中携带输入快照。</summary>
/// <param name="UserName">触发本次登录时捕获的用户名。</param>
/// <param name="Password">仅供本次登录使用的临时密码快照。</param>
public sealed record BenchLoginIntent(string UserName, string Password) : IMviIntent;
/// <summary>登录状态变化。</summary>
/// <param name="Busy">本次变化提交后的忙碌状态。</param>
public sealed record BenchLoginMutation(bool Busy) : IMviMutation<BenchLoginState>;
/// <summary>纯状态转换。</summary>
public sealed partial class BenchLoginReducer : MviReducerBase<BenchLoginState>
{
    [MviReduce(typeof(BenchLoginMutation))]
    private static BenchLoginState Apply(BenchLoginState state, BenchLoginMutation mutation)
        => state with { Busy = mutation.Busy, Completed = state.Completed + (mutation.Busy ? 0 : 1) };
}
/// <summary>不联网的同步服务路径，用来隔离框架成本。</summary>
public sealed class BenchLoginHandler : IIntentHandler<BenchLoginIntent, BenchLoginState>
{
    /// <summary>接纳、完成登录两次状态提交。</summary>
    /// <param name="intent">本次登录的输入快照。</param>
    /// <param name="context">本次意图的状态提交上下文。</param>
    /// <param name="cancellationToken">调用方取消标记。</param>
    /// <returns>同步完成的处理任务。</returns>
    public ValueTask HandleAsync(BenchLoginIntent intent, IIntentContext<BenchLoginState> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(context);

        if (context.TryReduce(static state => !state.Busy, new BenchLoginMutation(true))) context.Reduce(new BenchLoginMutation(false));
        return ValueTask.CompletedTask;
    }
}
/// <summary>与实际登录页相同的声明式输入和命令。</summary>
public sealed partial class BenchLoginViewModel : MviViewModelBase<BenchLoginState, BenchLoginIntent>
{
    /// <summary>获取或设置用户名。</summary>
    [MviBind] public partial string UserName { get; set; }
    /// <summary>获取或设置临时密码。</summary>
    [MviBind(Sensitive = true)] public partial string Password { get; set; }
    /// <summary>获取捕获两个输入的命令。</summary>
    [MviCommand(typeof(BenchLoginIntent), nameof(UserName), nameof(Password))]
    public partial IMviAsyncCommand SubmitCommand { get; }
}
