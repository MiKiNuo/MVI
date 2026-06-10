using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard;

/// <summary>
/// 表示 Dashboard 壳规约器。
/// <para>
/// <see cref="DashboardIntent.ShowPage"/> 不再携带 VM 对象，规约器仅把 PageKey/标题/说明
/// 写入 State；具体页面 VM 由 View 层通过 <see cref="IDashboardPageFactory"/> 解析。
/// </para>
/// </summary>
public sealed partial class DashboardReducer
    : MviReducerBase<DashboardState, DashboardIntent, DashboardEffect>
{
    /// <summary>
    /// 处理显示业务页面意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">显示页面意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<DashboardState, DashboardEffect> Reduce(
        DashboardState state,
        DashboardIntent.ShowPage intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<DashboardState, DashboardEffect>(state with
        {
            CurrentPageKey = intent.PageKey,
            CurrentPageTitle = intent.PageTitle,
            CurrentPageDescription = intent.PageDescription,
        });
    }
}
