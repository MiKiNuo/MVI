using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Menu;

/// <summary>
/// 表示 Dashboard 菜单意图处理器。
/// </summary>
public sealed class DashboardMenuIntentHandler
    : IMviIntentHandler<DashboardMenuState, DashboardMenuIntent, DashboardMenuMutation, DashboardMenuEffect>
{
    /// <summary>
    /// 处理意图产生变更与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>处理结果。</returns>
    public ValueTask<MviHandleResult<DashboardMenuMutation, DashboardMenuEffect>> HandleAsync(
        DashboardMenuState state,
        DashboardMenuIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        MviHandleResult<DashboardMenuMutation, DashboardMenuEffect> result = intent switch
        {
            DashboardMenuIntent.SelectMenuKey selectMenuKey => HandleSelectMenuKey(state, selectMenuKey),
            _ => MviHandleResult.Empty<DashboardMenuMutation, DashboardMenuEffect>(),
        };
        return new ValueTask<MviHandleResult<DashboardMenuMutation, DashboardMenuEffect>>(result);
    }

    private static MviHandleResult<DashboardMenuMutation, DashboardMenuEffect> HandleSelectMenuKey(
        DashboardMenuState state,
        DashboardMenuIntent.SelectMenuKey intent)
    {
        if (string.Equals(state.SelectedMenuKey, intent.SelectedMenuKey, StringComparison.Ordinal))
        {
            return MviHandleResult.Empty<DashboardMenuMutation, DashboardMenuEffect>();
        }

        DashboardMenuMutation[] mutations = new DashboardMenuMutation[]
        {
            new DashboardMenuMutation.SetSelectedMenuKey(intent.SelectedMenuKey),
            new DashboardMenuMutation.SetStatusText($"正在通过 Mediator 切换到：{intent.SelectedMenuKey}。"),
        };
        DashboardMenuEffect[] effects = new DashboardMenuEffect[]
        {
            new DashboardMenuEffect.RequestNavigation(intent.SelectedMenuKey),
        };
        return MviHandleResult.MutationsAndEffects<DashboardMenuMutation, DashboardMenuEffect>(mutations, effects);
    }
}
