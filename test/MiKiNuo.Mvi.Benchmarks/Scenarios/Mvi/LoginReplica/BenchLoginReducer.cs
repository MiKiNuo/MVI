using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.DI;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.LoginReplica;

/// <summary>
/// 表示登录复刻基准场景规约器：镜像真实登录示例的分支与守卫形态，经 [MviFeature] 编译期装配。
/// </summary>
[MviFeature]
public sealed partial class BenchLoginReducer
    : MviReducerBase<BenchLoginState, BenchLoginIntent, BenchLoginEffect>
{
    /// <summary>
    /// 处理用户名变更意图。
    /// </summary>
    [MviReduce(typeof(BenchLoginIntent.ChangeUserName))]
    private MviReduceResult<BenchLoginState, BenchLoginEffect> HandleChangeUserName(
        BenchLoginState state,
        BenchLoginIntent.ChangeUserName intent)
    {
        return Unchanged(state with
        {
            UserName = intent.UserName,
            ErrorMessage = null,
            CanSubmit = CanSubmit(intent.UserName, state.Password),
        });
    }

    /// <summary>
    /// 处理密码变更意图。
    /// </summary>
    [MviReduce(typeof(BenchLoginIntent.ChangePassword))]
    private MviReduceResult<BenchLoginState, BenchLoginEffect> HandleChangePassword(
        BenchLoginState state,
        BenchLoginIntent.ChangePassword intent)
    {
        return Unchanged(state with
        {
            Password = intent.Password,
            ErrorMessage = null,
            CanSubmit = CanSubmit(state.UserName, intent.Password),
        });
    }

    /// <summary>
    /// 处理提交登录意图：声明假认证副作用，凭证随 Effect 快照。
    /// </summary>
    [MviReduce(typeof(BenchLoginIntent.Submit), Guard = nameof(CanSubmitState))]
    private MviReduceResult<BenchLoginState, BenchLoginEffect> HandleSubmit(
        BenchLoginState state,
        BenchLoginIntent.Submit intent)
    {
        return WithEffect(
            state with { IsBusy = true, ErrorMessage = null, CanSubmit = false },
            new BenchLoginEffect.PerformLogin(state.UserName, state.Password));
    }

    /// <summary>
    /// 处理登录成功回流意图。
    /// </summary>
    [MviReduce(typeof(BenchLoginIntent.Succeeded))]
    private MviReduceResult<BenchLoginState, BenchLoginEffect> HandleSucceeded(
        BenchLoginState state,
        BenchLoginIntent.Succeeded intent)
    {
        return Unchanged(state with
        {
            IsBusy = false,
            ErrorMessage = null,
            DisplayName = intent.DisplayName,
        });
    }

    /// <summary>
    /// 处理登录失败回流意图。
    /// </summary>
    [MviReduce(typeof(BenchLoginIntent.Failed))]
    private MviReduceResult<BenchLoginState, BenchLoginEffect> HandleFailed(
        BenchLoginState state,
        BenchLoginIntent.Failed intent)
    {
        return Unchanged(state with
        {
            IsBusy = false,
            ErrorMessage = intent.ErrorMessage,
            CanSubmit = CanSubmit(state.UserName, state.Password),
        });
    }

    private bool CanSubmitState(BenchLoginState state)
    {
        return state.CanSubmit;
    }

    private bool CanSubmit(string userName, string password)
    {
        return !string.IsNullOrWhiteSpace(userName)
            && !string.IsNullOrWhiteSpace(password);
    }
}
