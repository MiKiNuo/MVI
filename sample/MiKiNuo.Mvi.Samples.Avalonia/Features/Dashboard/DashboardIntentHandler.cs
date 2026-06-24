using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard;

/// <summary>
/// 表示 Dashboard 壳意图处理器。
/// </summary>
public sealed class DashboardIntentHandler
    : IMviIntentHandler<DashboardState, DashboardIntent, DashboardMutation, DashboardEffect>
{
    /// <summary>
    /// 处理意图产生变更与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>处理结果。</returns>
    public ValueTask<MviHandleResult<DashboardMutation, DashboardEffect>> HandleAsync(
        DashboardState state,
        DashboardIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        MviHandleResult<DashboardMutation, DashboardEffect> result = intent switch
        {
            DashboardIntent.ShowPage showPage => HandleShowPage(showPage),
            _ => MviHandleResult.Empty<DashboardMutation, DashboardEffect>(),
        };
        return new ValueTask<MviHandleResult<DashboardMutation, DashboardEffect>>(result);
    }

    private static MviHandleResult<DashboardMutation, DashboardEffect> HandleShowPage(
        DashboardIntent.ShowPage intent)
    {
        return MviHandleResult.Mutations<DashboardMutation, DashboardEffect>(
            new DashboardMutation.SetCurrentPageKey(intent.PageKey),
            new DashboardMutation.SetCurrentPageTitle(intent.PageTitle),
            new DashboardMutation.SetCurrentPageDescription(intent.PageDescription));
    }
}
