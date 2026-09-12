using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.DI;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Home;

/// <summary>
/// 表示主页规约器。
/// </summary>
[MviFeature]
public sealed partial class HomeReducer
    : MviReducerBase<HomeState, HomeIntent, HomeEffect>
{
    /// <summary>接收主页自己的显示数据。</summary>
    [MviReduce(typeof(HomeIntent.ShowUser))]
    private MviReduceResult<HomeState, HomeEffect> HandleShowUser(HomeState state, HomeIntent.ShowUser intent)
        => Unchanged(state with { DisplayName = intent.DisplayName });
    /// <summary>
    /// 处理退出登录意图：声明跳转登录页副作用。
    /// </summary>
    [MviReduce(typeof(HomeIntent.Logout))]
    private MviReduceResult<HomeState, HomeEffect> HandleLogout(
        HomeState state,
        HomeIntent.Logout intent)
    {
        return WithEffect(HomeState.Initial, new HomeEffect.ShowLoginPage());
    }
}
